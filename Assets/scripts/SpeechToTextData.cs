using System;

// 這裡定義了 Data 命名空間，解決您的報錯
namespace GoogleSpeechToText.Scripts.Data
{
    // 對應 Google STT 回傳的總表
    [Serializable]
    public class SpeechToTextResponse
    {
        public Result[] results;
        public Error error; // 如果出錯，Google 會回傳這個
    }

    // 辨識結果的陣列
    [Serializable]
    public class Result
    {
        public Alternative[] alternatives;
    }

    // 每一句可能的辨識結果 (我們通常取第一個)
    [Serializable]
    public class Alternative
    {
        public string transcript; // 這是最重要的：辨識出來的文字
        public float confidence;  // 信心分數 (0~1)
    }

    // 錯誤訊息結構 (解決 Data.Error 的報錯)
    [Serializable]
    public class Error
    {
        public int code;        // 例如 400, 403, 404
        public string message;  // 錯誤原因描述
        public string status;
    }
}