using System.Text;

namespace Cathedral.Glyph.Microworld.LocationSystem
{
    /// <summary>
    /// Logs all LLM interactions during gameplay for debugging and analysis
    /// </summary>
    public class GameplayLogger
    {
        private readonly string _logFilePath;
        private readonly StringBuilder _logBuffer;
        private int _turnNumber = 0;
        private int _requestNumber = 0;

        public GameplayLogger(string sessionId)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _logFilePath = Path.Combine("logs", $"gameplay_session_{sessionId}_{timestamp}.log");
            _logBuffer = new StringBuilder();
            
            // Create logs directory if it doesn't exist
            Directory.CreateDirectory("logs");
            
            // Initialize log file
            WriteHeader();
        }

        private void WriteHeader()
        {
            _logBuffer.AppendLine("=".PadRight(80, '='));
            _logBuffer.AppendLine("CATHEDRAL INTERACTIVE FOREST ADVENTURE - LLM SESSION LOG");
            _logBuffer.AppendLine($"Session Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _logBuffer.AppendLine("=".PadRight(80, '='));
            _logBuffer.AppendLine();
            FlushToFile();
        }

        public void LogTurnStart(int turnNumber, string currentSublocation, Dictionary<string, string> currentStates)
        {
            _turnNumber = turnNumber;
            _logBuffer.AppendLine($"--- TURN {turnNumber} START ---");
            _logBuffer.AppendLine($"Current Location: {currentSublocation}");
            _logBuffer.AppendLine("Current States:");
            foreach (var (category, state) in currentStates)
            {
                _logBuffer.AppendLine($"  - {category}: {state}");
            }
            _logBuffer.AppendLine();
            FlushToFile();
        }

        public void LogDirectorRequest(string prompt, string gbnf)
        {
            _requestNumber++;
            _logBuffer.AppendLine($"🎲 DIRECTOR REQUEST #{_requestNumber} (Turn {_turnNumber})");
            _logBuffer.AppendLine($"Timestamp: {DateTime.Now:HH:mm:ss.fff}");
            _logBuffer.AppendLine("PROMPT:");
            _logBuffer.AppendLine("-".PadRight(60, '-'));
            _logBuffer.AppendLine(prompt);
            _logBuffer.AppendLine("-".PadRight(60, '-'));
            _logBuffer.AppendLine("GBNF GRAMMAR:");
            _logBuffer.AppendLine("-".PadRight(40, '-'));
            _logBuffer.AppendLine(gbnf);
            _logBuffer.AppendLine("-".PadRight(40, '-'));
            _logBuffer.AppendLine();
            FlushToFile();
        }

        public void LogDirectorResponse(string response, TimeSpan responseTime, bool isValid, List<string> validationErrors)
        {
            _logBuffer.AppendLine($"🎲 DIRECTOR RESPONSE #{_requestNumber}");
            _logBuffer.AppendLine($"Response Time: {responseTime.TotalMilliseconds:F2}ms");
            _logBuffer.AppendLine($"Validation: {(isValid ? "✓ VALID" : "✗ INVALID")}");
            
            if (!isValid && validationErrors.Any())
            {
                _logBuffer.AppendLine("Validation Errors:");
                foreach (var error in validationErrors.Take(5))
                {
                    _logBuffer.AppendLine($"  - {error}");
                }
            }

            _logBuffer.AppendLine("RESPONSE:");
            _logBuffer.AppendLine("-".PadRight(60, '-'));
            _logBuffer.AppendLine(response);
            _logBuffer.AppendLine("-".PadRight(60, '-'));
            _logBuffer.AppendLine();
            FlushToFile();
        }

        public void LogNarratorRequest(string prompt, string gbnf)
        {
            _requestNumber++;
            _logBuffer.AppendLine($"📖 NARRATOR REQUEST #{_requestNumber} (Turn {_turnNumber})");
            _logBuffer.AppendLine($"Timestamp: {DateTime.Now:HH:mm:ss.fff}");
            _logBuffer.AppendLine("PROMPT:");
            _logBuffer.AppendLine("-".PadRight(60, '-'));
            _logBuffer.AppendLine(prompt);
            _logBuffer.AppendLine("-".PadRight(60, '-'));
            _logBuffer.AppendLine("GBNF GRAMMAR:");
            _logBuffer.AppendLine("-".PadRight(40, '-'));
            _logBuffer.AppendLine(gbnf);
            _logBuffer.AppendLine("-".PadRight(40, '-'));
            _logBuffer.AppendLine();
            FlushToFile();
        }

        public void LogNarratorResponse(string response, TimeSpan responseTime)
        {
            _logBuffer.AppendLine($"📖 NARRATOR RESPONSE #{_requestNumber}");
            _logBuffer.AppendLine($"Response Time: {responseTime.TotalMilliseconds:F2}ms");
            _logBuffer.AppendLine("RESPONSE:");
            _logBuffer.AppendLine("-".PadRight(60, '-'));
            _logBuffer.AppendLine(response);
            _logBuffer.AppendLine("-".PadRight(60, '-'));
            _logBuffer.AppendLine();
            FlushToFile();
        }

        public void LogPlayerAction(int actionChoice, string actionText)
        {
            _logBuffer.AppendLine($"🎯 PLAYER ACTION (Turn {_turnNumber})");
            _logBuffer.AppendLine($"Chosen Action #{actionChoice}: {actionText}");
            _logBuffer.AppendLine();
            FlushToFile();
        }

        public void LogActionOutcome(PlayerAction outcome)
        {
            _logBuffer.AppendLine($"⚡ ACTION OUTCOME (Turn {_turnNumber})");
            _logBuffer.AppendLine($"Action: {outcome.ActionText}");
            _logBuffer.AppendLine($"Result: {(outcome.WasSuccessful ? "SUCCESS" : "FAILURE")}");
            _logBuffer.AppendLine($"Outcome: {outcome.Outcome}");
            
            if (outcome.StateChanges.Any())
            {
                _logBuffer.AppendLine("State Changes:");
                foreach (var (category, newState) in outcome.StateChanges)
                {
                    _logBuffer.AppendLine($"  - {category} → {newState}");
                }
            }

            if (!string.IsNullOrEmpty(outcome.NewSublocation))
            {
                _logBuffer.AppendLine($"Location Change: → {outcome.NewSublocation}");
            }

            if (!string.IsNullOrEmpty(outcome.ItemGained) && outcome.ItemGained != "none")
            {
                _logBuffer.AppendLine($"Item Gained: {outcome.ItemGained}");
            }

            if (!string.IsNullOrEmpty(outcome.CompanionGained) && outcome.CompanionGained != "none")
            {
                _logBuffer.AppendLine($"Companion Gained: {outcome.CompanionGained}");
            }

            _logBuffer.AppendLine();
            FlushToFile();
        }

        public void LogGameEnd(string reason)
        {
            _logBuffer.AppendLine("=".PadRight(80, '='));
            _logBuffer.AppendLine("GAME SESSION ENDED");
            _logBuffer.AppendLine($"End Reason: {reason}");
            _logBuffer.AppendLine($"Total Turns: {_turnNumber}");
            _logBuffer.AppendLine($"Total LLM Requests: {_requestNumber}");
            _logBuffer.AppendLine($"Session Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _logBuffer.AppendLine("=".PadRight(80, '='));
            FlushToFile();
        }

        private void FlushToFile()
        {
            try
            {
                File.AppendAllText(_logFilePath, _logBuffer.ToString());
                _logBuffer.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to write to log file: {ex.Message}");
            }
        }

        public string GetLogFilePath() => _logFilePath;
    }
}