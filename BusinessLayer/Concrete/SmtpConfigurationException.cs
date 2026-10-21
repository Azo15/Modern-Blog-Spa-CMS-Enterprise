namespace BusinessLayer.Concrete;

public sealed class SmtpConfigurationException(string message) : InvalidOperationException(message);
