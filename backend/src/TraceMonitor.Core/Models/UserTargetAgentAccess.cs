namespace TraceMonitor.Core.Models;

/// <summary>Grants a non-admin User visibility into one specific (Target, Agent) pair — the RBAC
/// grain is the pair, not Target or Agent alone, since the same Target can be visible from one
/// Agent's perspective but not another's for a given user. Admins bypass this check entirely.</summary>
public class UserTargetAgentAccess
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int TargetId { get; set; }
    public Target? Target { get; set; }
    public int AgentId { get; set; }
    public Agent? Agent { get; set; }
}
