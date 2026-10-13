namespace BusinessLayer.Dtos;

public enum ReportKind { Contacts = 1, Blogs = 2, Interactions = 3, Users = 4 }
public enum ReportFormat { Excel = 1, Pdf = 2 }

/// <summary>Dates are UTC; FromUtc is inclusive and ToUtc is exclusive.</summary>
public sealed record ReportFilter(DateTime? FromUtc = null, DateTime? ToUtc = null, int? UserId = null, int? BlogId = null);
public sealed record ReportFile(byte[] Content, string ContentType, string FileName, int RowCount);
