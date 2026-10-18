namespace WebUI.Models;
public sealed record SidebarLink(string Controller,string Action,string Label,bool Ai=false);
public sealed record SidebarGroupModel(string Id,string Title,string Icon,IReadOnlyList<SidebarLink> Links);
