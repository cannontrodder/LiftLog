using LiftLog.Lib.Models;
using LiftLog.Ui.Models;

namespace LiftLog.Ui.Store.CurrentSession;

public enum SessionTarget
{
    WorkoutSession,
    HistorySession
}

public record CycleExerciseRepsAction(SessionTarget Target, int ExerciseIndex, int SetIndex);

public record ClearExerciseRepsAction(SessionTarget Target, int ExerciseIndex, int SetIndex);

public record UpdateExerciseWeightAction(SessionTarget Target, int ExerciseIndex, decimal Kilograms);

public record EditExerciseInActiveSessionAction(SessionTarget Target, int ExerciseIndex, SessionExerciseEditModel Exercise);

public record AddExerciseToActiveSessionAction(SessionTarget Target, SessionExerciseEditModel Exercise);

public record RemoveExerciseFromActiveSessionAction(SessionTarget Target, int ExerciseIndex);

public record SetCurrentSessionAction(SessionTarget Target, Session? Session);

public record PersistCurrentSessionAction(SessionTarget Target);

public record RehydrateSessionAction();

public record NotifySetTimerAction(SessionTarget Target);

public record SetLatestSetTimerNotificationIdAction(Guid Id);

public record CompleteSetFromNotificationAction(SessionTarget Target);

public record DeleteSessionAction(Session Session);