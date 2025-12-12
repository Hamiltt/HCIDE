using System;

namespace HCIDE.Models
{
    /// <summary>
    /// Настройки цветовой темы редактора
    /// </summary>
    public class ThemeSettings
    {
        // === Основные цвета редактора ===

        /// <summary>
        /// Цвет фона редактора
        /// </summary>
        public string EditorBackground { get; set; } = "#1E1E1E";

        /// <summary>
        /// Цвет текста в редакторе
        /// </summary>
        public string EditorForeground { get; set; } = "#D4D4D4";

        /// <summary>
        /// Цвет номеров строк
        /// </summary>
        public string LineNumberForeground { get; set; } = "#858585";

        /// <summary>
        /// Фон выделенного текста
        /// </summary>
        public string SelectionBackground { get; set; } = "#264F78";

        /// <summary>
        /// Цвет каретки (курсора)
        /// </summary>
        public string CaretColor { get; set; } = "#FFFFFF";

        // === Цвета синтаксиса ===

        /// <summary>
        /// Цвет ключевых слов (def, if, class, function и т.д.)
        /// </summary>
        public string KeywordColor { get; set; } = "#569CD6";

        /// <summary>
        /// Цвет строковых литералов
        /// </summary>
        public string StringColor { get; set; } = "#CE9178";

        /// <summary>
        /// Цвет комментариев
        /// </summary>
        public string CommentColor { get; set; } = "#6A9955";

        /// <summary>
        /// Цвет чисел
        /// </summary>
        public string NumberColor { get; set; } = "#B5CEA8";

        /// <summary>
        /// Цвет имен функций
        /// </summary>
        public string FunctionColor { get; set; } = "#DCDCAA";

        /// <summary>
        /// Цвет имен классов
        /// </summary>
        public string ClassColor { get; set; } = "#4EC9B0";

        /// <summary>
        /// Цвет переменных
        /// </summary>
        public string VariableColor { get; set; } = "#9CDCFE";

        /// <summary>
        /// Цвет операторов (+, -, *, / и т.д.)
        /// </summary>
        public string OperatorColor { get; set; } = "#D4D4D4";

        // === Настройки шрифта ===

        /// <summary>
        /// Семейство шрифта
        /// </summary>
        public string FontFamily { get; set; } = "Consolas";

        /// <summary>
        /// Размер шрифта
        /// </summary>
        public int FontSize { get; set; } = 14;

        /// <summary>
        /// Жирность шрифта (Normal, Bold)
        /// </summary>
        public string FontWeight { get; set; } = "Normal";

        // === Настройки интерфейса ===

        /// <summary>
        /// Цвет фона панелей
        /// </summary>
        public string PanelBackground { get; set; } = "#252526";

        /// <summary>
        /// Цвет границ
        /// </summary>
        public string BorderColor { get; set; } = "#3E3E42";

        /// <summary>
        /// Цвет статус-бара
        /// </summary>
        public string StatusBarBackground { get; set; } = "#007ACC";

        /// <summary>
        /// Создать тему по умолчанию (VS Code Dark)
        /// </summary>
        public static ThemeSettings CreateDefault() => new();

        /// <summary>
        /// Создать случайную цветовую тему
        /// </summary>
        public static ThemeSettings CreateRandomized()
        {
            var theme = new ThemeSettings();
            var random = Random.Shared;

            // Генерируем случайные цвета для основных элементов
            theme.EditorBackground = GenerateRandomColor(random, 20, 40);      // Темный фон
            theme.EditorForeground = GenerateRandomColor(random, 180, 255);    // Светлый текст
            theme.KeywordColor = GenerateRandomColor(random, 80, 200);
            theme.StringColor = GenerateRandomColor(random, 150, 220);
            theme.CommentColor = GenerateRandomColor(random, 100, 150);
            theme.NumberColor = GenerateRandomColor(random, 150, 220);
            theme.FunctionColor = GenerateRandomColor(random, 180, 240);
            theme.ClassColor = GenerateRandomColor(random, 100, 200);
            theme.SelectionBackground = GenerateRandomColor(random, 40, 100);

            return theme;
        }

        /// <summary>
        /// Генерация случайного HEX цвета с контролем яркости
        /// </summary>
        private static string GenerateRandomColor(Random random, int minBrightness, int maxBrightness)
        {
            int r = random.Next(minBrightness, maxBrightness);
            int g = random.Next(minBrightness, maxBrightness);
            int b = random.Next(minBrightness, maxBrightness);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        /// <summary>
        /// Клонировать настройки темы
        /// </summary>
        public ThemeSettings Clone()
        {
            return new ThemeSettings
            {
                EditorBackground = this.EditorBackground,
                EditorForeground = this.EditorForeground,
                LineNumberForeground = this.LineNumberForeground,
                SelectionBackground = this.SelectionBackground,
                CaretColor = this.CaretColor,
                KeywordColor = this.KeywordColor,
                StringColor = this.StringColor,
                CommentColor = this.CommentColor,
                NumberColor = this.NumberColor,
                FunctionColor = this.FunctionColor,
                ClassColor = this.ClassColor,
                VariableColor = this.VariableColor,
                OperatorColor = this.OperatorColor,
                FontFamily = this.FontFamily,
                FontSize = this.FontSize,
                FontWeight = this.FontWeight,
                PanelBackground = this.PanelBackground,
                BorderColor = this.BorderColor,
                StatusBarBackground = this.StatusBarBackground
            };
        }
    }
}