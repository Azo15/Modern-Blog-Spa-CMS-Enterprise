using System.Net;
using System.Text.Json;
using BusinessLayer.Concrete;
using BusinessLayer.Dtos;
using Microsoft.Extensions.Configuration;
namespace BusinessLayer.Tests;
public sealed class AiBlogServiceTests
{
    private sealed class Transport(string body,HttpStatusCode status=HttpStatusCode.OK) : HttpMessageHandler
    {
        public int Calls {get;private set;}
        public string? Payload {get;private set;}
        public string? Authorization {get;private set;}
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;Assert.Equal("https://api.openai.com/v1/responses",request.RequestUri!.ToString());
            Authorization=request.Headers.Authorization?.Parameter;Payload=await request.Content!.ReadAsStringAsync(token);
            return new(status){Content=new StringContent(body)};
        }
    }
    private static readonly AiBlogPrompt Prompt=new("Hamam ritüelleri","Wellness","tr-TR","luxury");
    private static IConfiguration Config(bool enabled=true)=>new ConfigurationBuilder().AddInMemoryCollection(enabled?new Dictionary<string,string?>{["Ai:ApiKey"]="test-key-only",["Ai:Model"]="test-model"}:[]).Build();
    [Fact]
    public async Task StructuredOutputIsSanitizedAndRequestDoesNotStoreConversation()
    {
        var draft=new {title="Hamam ritüelleri rehberi",subtitle="Dingin bir ritüel için özenli hazırlık",summary="Hamam deneyimini sakin ve özenli bir hazırlıkla keşfetmek için temel bir rehber.",html="<h2>Ritüel</h2><p>İçerik</p><script>alert(1)</script><img src=x onerror=alert(1)>"};
        var body=JsonSerializer.Serialize(new{status="completed",output=new[]{new{type="message",content=new[]{new{type="output_text",text=JsonSerializer.Serialize(draft)}}}}});
        var transport=new Transport(body);using var client=new HttpClient(transport);
        var result=await new AiBlogService(client,Config(),new HtmlContentSanitizer()).GenerateAsync(Prompt);
        Assert.Equal(draft.title,result.Title);Assert.DoesNotContain("<script",result.Html);Assert.DoesNotContain("onerror",result.Html);
        using var payload=JsonDocument.Parse(transport.Payload!);Assert.False(payload.RootElement.GetProperty("store").GetBoolean());
        Assert.Equal("json_schema",payload.RootElement.GetProperty("text").GetProperty("format").GetProperty("type").GetString());
        Assert.Equal("test-key-only",transport.Authorization);
    }
    [Fact]
    public async Task MissingConfigurationNeverMakesNetworkRequest()
    {
        var transport=new Transport("{}");using var client=new HttpClient(transport);
        await Assert.ThrowsAsync<AiBlogUnavailableException>(()=>new AiBlogService(client,Config(false),new HtmlContentSanitizer()).GenerateAsync(Prompt));Assert.Equal(0,transport.Calls);
    }
    [Theory]
    [InlineData("not json")] [InlineData("{\"status\":\"incomplete\"}")] [InlineData("{\"status\":\"completed\",\"output\":[]}")]
    public async Task MalformedOrIncompleteProviderResponsesCannotBecomeDrafts(string body)
    {
        using var client=new HttpClient(new Transport(body));await Assert.ThrowsAsync<AiBlogUnavailableException>(()=>new AiBlogService(client,Config(),new HtmlContentSanitizer()).GenerateAsync(Prompt));
    }
    [Fact]
    public async Task ProviderErrorsNeverExposeRawResponseSecrets()
    {
        using var client=new HttpClient(new Transport("private-provider-error-secret",HttpStatusCode.Unauthorized));
        var error=await Assert.ThrowsAsync<AiBlogUnavailableException>(()=>new AiBlogService(client,Config(),new HtmlContentSanitizer()).GenerateAsync(Prompt));Assert.DoesNotContain("private-provider",error.Message);
    }
}
