
public class AppLogger {

  public static bool RunInNonInteractiveBatchMode = false;

  public static void SetNonInteractiveBatchMode() {
    RunInNonInteractiveBatchMode = true;
  }

  public static void LogSolution(string Message) {
    Console.WriteLine();
    Console.WriteLine(Message);
    Console.Write(new string('-', Message.Length));
  }

  public static void LogStep(string Message) {
    Console.WriteLine();
    Console.WriteLine(" > " + Message);
  }

  public static void LogSubstep(string Message) {
    Console.WriteLine("   - " + Message);
  }

  public static void LogSubOperationStart(string Message) {
    Console.Write("   - " + Message);
  }

  public static void LogOperationStart(string Message) {
    Console.WriteLine();
    Console.Write(" > " + Message);
  }

  public static void LogOperationInProgress() {
    Console.Write(".");
  }

  public static void LogOperationComplete() {
    Console.WriteLine();
  }

  private static int TableWidth = 120;

  public static void LogTableHeader(string TableTitle) {
    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine($" > {TableTitle}");
    Console.WriteLine("   " + new string('-', TableWidth-3));
  }

  public static void LogTableRow(string FirstColumnValue, string SecondColumnValue) {

    int firstColumnWidth = 18;
    int firstColumnValueLength = FirstColumnValue.Length;
    int firstColumnOffset = firstColumnWidth - firstColumnValueLength;

    int secondColumnWidth = TableWidth - firstColumnWidth;
    int secondColumnValueLength = SecondColumnValue.Length;
    int secondColumnOffset = secondColumnWidth - secondColumnValueLength - 8;

    string row = $"   | {FirstColumnValue}{new string(' ', firstColumnOffset)}| {SecondColumnValue}{new string(' ', secondColumnOffset)}|";
    Console.WriteLine(row);
    Console.WriteLine("   " + new string('-', TableWidth-3));

  }

  public static void PromptUserToContinue(string Message = "Press ENTER to open workspace in the browser") {
    if (!RunInNonInteractiveBatchMode) {
      LogOperationStart(Message);
      Console.ReadLine();
      AppLogger.LogOperationComplete();
      AppLogger.LogOperationComplete();
    }
    else {
      AppLogger.LogOperationComplete();
    }
  }

  public static void LogException(Exception ex) {
    var originalColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine();
    Console.WriteLine($"ERROR: {ex.GetType().ToString()}");
    Console.WriteLine(ex.Message);
    Console.WriteLine();
    Console.ForegroundColor = originalColor;
  }
}
