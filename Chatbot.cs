using System;
using System.IO;

namespace CybersecurityChatbott
{
    public class ChatBot
    {
        private KeywordResponder _keywords;
        private SentimentDetector _sentiment;
        private MemoryStore _memory;
        private string _lastTopic = null;

        private bool _awaitingName = true;

        private string DefaultMemoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "cyber_memory.json");

        public ChatBot()
        {
            _keywords = new KeywordResponder();
            _sentiment = new SentimentDetector();
            _memory = new MemoryStore();

            // Try to load existing memory silently
            try
            {
                if (_memory.LoadFromFile(DefaultMemoryPath))
                {
                    // if we have a username already, we are not awaiting it
                    _awaitingName = string.IsNullOrEmpty(_memory.UserName);
                }
            }
            catch
            {
                // ignore load errors
            }
        }

        // Expose username for UI to build user-specific file names
        public string UserName => _memory == null ? string.Empty : _memory.UserName;

        public string GetGreeting()
        {
            if (!string.IsNullOrEmpty(_memory.UserName) && !_awaitingName)
            {
                return $"Welcome back, {_memory.UserName}! Ask me about cybersecurity.";
            }

            return "Hello! Welcome to the Cybersecurity Chatbot. What is your name?";
        }

        public string ProcessInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = input.Trim();

            // Commands start with '/'
            if (input.StartsWith("/"))
            {
                return HandleCommand(input);
            }

            string lower = input.ToLower();
            if (lower.Contains("exit") || lower.Contains("quit") || lower.Contains("bye"))
            {
                // Return a friendly shutdown message; don't call Application.Current.Shutdown here
                // so UI-layer can decide how to handle application shutdown.
                return "Goodbye!";
            }

            // Capture user name first
            if (_awaitingName)
            {
                _memory.UserName = input;
                _awaitingName = false;

                return "Nice to meet you " + _memory.UserName + ". Ask me about cybersecurity.";
            }

            // Follow-up requests (e.g., "tell me more", "explain more") should return
            // additional information about the last topic without asking the user to repeat it.
            if (lower.Contains("tell me more") || lower.Contains("explain more") || lower.Contains("more about"))
            {
                if (!string.IsNullOrEmpty(_lastTopic))
                {
                    string matched;
                    string followUp = _keywords.GetResponseWithKey(_lastTopic, out matched);
                    if (!string.IsNullOrEmpty(followUp))
                        return _memory.GetPersonalisedOpener() + followUp;

                    return "I don't have more info on that yet. Try asking about passwords, phishing or malware.";
                }

                return "What topic would you like to hear more about?";
            }

            // EXPLAIN / STEPS requests: e.g. "explain passwords", "give steps for passwords"
            if (lower.StartsWith("explain ") || lower.StartsWith("explain") || lower.Contains(" steps ") || lower.Contains("give steps") || lower.Contains("how to "))
            {
                // try to extract a noun/topic after the phrase
                string topic = null;

                // common patterns
                var patterns = new[] { "explain ", "explain about ", "give steps for ", "steps for ", "how to ", "how do i ", "how can i " };
                foreach (var p in patterns)
                {
                    int idx = lower.IndexOf(p, StringComparison.Ordinal);
                    if (idx >= 0)
                    {
                        topic = lower.Substring(idx + p.Length).Trim();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(topic))
                {
                    // as a fallback, try last topic
                    topic = _lastTopic;
                }

                if (!string.IsNullOrEmpty(topic))
                {
                    // remove trailing punctuation
                    topic = topic.Trim().TrimEnd('.', '?', '!');

                    string matchedTopicKey;
                    string resp = _keywords.GetResponseWithKey(topic, out matchedTopicKey);
                    if (!string.IsNullOrEmpty(resp))
                    {
                        _lastTopic = matchedTopicKey?.ToLower() ?? topic.ToLower();
                        return _memory.GetPersonalisedOpener() + resp;
                    }

                    // try simpler token (first word)
                    var first = topic.Split(' ')[0];
                    resp = _keywords.GetResponseWithKey(first, out matchedTopicKey);
                    if (!string.IsNullOrEmpty(resp))
                    {
                        _lastTopic = matchedTopicKey?.ToLower() ?? first.ToLower();
                        return _memory.GetPersonalisedOpener() + resp;
                    }

                    return "I couldn't find specific steps for '" + topic + "'. Try asking about passwords, phishing or malware.";
                }
            }

            // Simple/fallback intents
            if (lower.Contains("how are you"))
            {
                return "I'm operating well and ready to help with cybersecurity awareness.";
            }

            if (lower.Contains("what can you do") || lower.Contains("help"))
            {
                return "I can answer questions about cybersecurity, provide tips, and help you stay informed about the latest threats. Try asking about passwords, phishing or privacy.";
            }

            if (lower.Contains("purpose"))
            {
                return "My purpose is to educate and assist users in understanding cybersecurity concepts, best practices, and how to protect themselves online.";
            }

            if (lower.Contains("i like privacy") || lower.Contains("like privacy"))
            {
                _memory.FavouriteTopic = "privacy";
                return "Great! I'll remember that privacy interests you.";
            }

            if (lower.Contains("i like phishing") || lower.Contains("like phishing"))
            {
                _memory.FavouriteTopic = "phishing";
                return "Great! I'll remember that phishing interests you.";
            }

            if (lower.Contains("i like passwords") || lower.Contains("like passwords"))
            {
                _memory.FavouriteTopic = "passwords";
                return "Great! I'll remember that passwords interest you.";
            }

            // Forsee sentiment
            Sentiment mood = _sentiment.Detect(input);

            string moodMessage = string.Empty;

            switch (mood)
            {
                case Sentiment.Worried:
                    moodMessage = "I understand your concern. ";
                    break;
                case Sentiment.Frustrated:
                    moodMessage = "I understand this can feel frustrating. ";
                    break;
                case Sentiment.Happy:
                    moodMessage = "I'm glad you're feeling positive. ";
                    break;
            }

            // Handle protection follow-ups: "how can I protect..." or "how to protect..."
            if (lower.Contains("how can i protect") || lower.Contains("how to protect") || lower.Contains("protect myself") || lower.Contains("ways to protect") || lower.Contains("how do i protect"))
            {
                // try to extract topic after "from"
                string topic = null;
                int fromIdx = lower.IndexOf(" from ", StringComparison.Ordinal);
                if (fromIdx >= 0)
                {
                    topic = lower.Substring(fromIdx + 6).Trim(); // after ' from '
                    // remove trailing punctuation
                    topic = topic.Trim().TrimEnd('.', '?', '!');
                }

                if (string.IsNullOrEmpty(topic) && !string.IsNullOrEmpty(_lastTopic))
                {
                    topic = _lastTopic;
                }

                if (string.IsNullOrEmpty(topic))
                {
                    // fallback to general protection advice
                    string protectReply = _keywords.GetResponse("protect");
                    return _memory.GetPersonalisedOpener() + moodMessage + protectReply;
                }

                // First try to provide a definition for the topic, if available
                string defMatched;
                string definition = _keywords.GetResponseWithKey($"what is {topic}", out defMatched);
                if (definition == null)
                    definition = _keywords.GetResponseWithKey($"define {topic}", out defMatched);

                // Then get protection steps for the raw topic (e.g., "malware" or "phishing")
                string protMatched;
                string protectionReply = _keywords.GetResponseWithKey(topic, out protMatched);

                // Build response: definition first (if any), then protection steps
                string result = _memory.GetPersonalisedOpener() + moodMessage;
                if (!string.IsNullOrEmpty(definition))
                {
                    result += definition;
                    // separate sections
                    result += "\n\n";
                }

                if (!string.IsNullOrEmpty(protectionReply))
                {
                    result += protectionReply;
                }
                else
                {
                    // fallback to general protection
                    result += _keywords.GetResponse("protect");
                }

                // update last topic
                _lastTopic = topic.ToLower();

                return result;
            }

            // Check keyword response (general)
            string matchedKey;
            string keywordReply = _keywords.GetResponseWithKey(input, out matchedKey);

            if (keywordReply != null)
            {
                // set last topic for follow-ups (normalize certain keys)
                try
                {
                    if (!string.IsNullOrEmpty(matchedKey))
                    {
                        string keyLower = matchedKey.ToLower();
                        if (keyLower.StartsWith("what is "))
                            _lastTopic = keyLower.Substring(8).Trim();
                        else if (keyLower.StartsWith("define "))
                            _lastTopic = keyLower.Substring(7).Trim();
                        else
                            _lastTopic = keyLower;
                    }
                }
                catch
                {
                    _lastTopic = null;
                }

                string personalMessage = _memory.GetPersonalisedOpener();
                return personalMessage + moodMessage + keywordReply;
            }

            // Default response
            return "I'm not sure I understand. Try rephrasing or type /help for commands.";
        }

        private string HandleCommand(string command)
        {
            var cmd = command.Trim().ToLower();

            if (cmd == "/help")
            {
                return "Commands: /help, /reset, /save, /load, /whoami";
            }

            if (cmd == "/reset")
            {
                _memory = new MemoryStore();
                _awaitingName = true;
                try { File.Delete(DefaultMemoryPath); } catch { }
                return "Memory reset. What's your name?";
            }

            if (cmd == "/save")
            {
                try
                {
                    _memory.SaveToFile(DefaultMemoryPath);
                    return $"Memory saved to {DefaultMemoryPath}";
                }
                catch (Exception ex)
                {
                    return "Failed to save memory: " + ex.Message;
                }
            }

            if (cmd == "/load")
            {
                try
                {
                    if (_memory.LoadFromFile(DefaultMemoryPath))
                    {
                        _awaitingName = string.IsNullOrEmpty(_memory.UserName);
                        return "Memory loaded.";
                    }

                    return "No saved memory found.";
                }
                catch (Exception ex)
                {
                    return "Failed to load memory: " + ex.Message;
                }
            }

            if (cmd == "/whoami")
            {
                if (!string.IsNullOrEmpty(_memory.UserName))
                    return "You are " + _memory.UserName + ".";
                return "I don't know your name yet.";
            }

            return "Unknown command. Type /help for a list of commands.";
        }
    }
}
