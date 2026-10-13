namespace HandleScope.Presentation.Abstract;

/// <summary>Asks the user to confirm termination of a selected process.</summary>
public interface IProcessTerminationConfirmationService
{
    /// <summary>Returns true only when the user confirms termination.</summary>
    bool Confirm(string processName, int processId);
}
