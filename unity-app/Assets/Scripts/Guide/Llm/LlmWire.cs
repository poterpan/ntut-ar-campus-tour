using System;
using UnityEngine;

namespace NtutAR.Guide
{
    /// <summary>OpenAI-compatible chat/completions 的 JsonUtility wire 格式 + 純函數解析(可測)。</summary>
    public static class LlmWire
    {
        [Serializable]
        public class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        public class ChatCompletionRequest
        {
            public string model;
            public ChatMessage[] messages;
            public int max_tokens;
        }

        [Serializable]
        public class ChatChoice
        {
            public ChatMessage message;
        }

        [Serializable]
        public class ChatCompletionResponse
        {
            public ChatChoice[] choices;
        }

        public static string BuildRequestJson(string model, string systemPrompt, string question, int maxTokens)
        {
            var body = new ChatCompletionRequest
            {
                model = model,
                messages = new[]
                {
                    new ChatMessage { role = "system", content = systemPrompt },
                    new ChatMessage { role = "user", content = question }
                },
                max_tokens = maxTokens
            };
            return JsonUtility.ToJson(body);
        }

        /// <summary>從回應 JSON 取第一個 choice 的文字;格式不符回 null。</summary>
        public static string ExtractAnswer(string responseJson)
        {
            if (string.IsNullOrEmpty(responseJson)) return null;
            ChatCompletionResponse response;
            try
            {
                response = JsonUtility.FromJson<ChatCompletionResponse>(responseJson);
            }
            catch (Exception)
            {
                return null;
            }
            if (response?.choices == null || response.choices.Length == 0) return null;
            var content = response.choices[0]?.message?.content;
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
    }
}
