namespace HelpDesk.Api.Records;

public record TicketRecord(
    int Id, string User, DateTime DateOfIssue, 
    DateTime DateOfDeadline, string Description
    );