using CodeCracker.CSharp.Usage.MethodAnalyzers;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ValidateColorAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ValidateColorAnalyzer_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Message = new LocalizableResourceString(nameof(Resources.ValidateColorAnalyzer_Message), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ValidateColorAnalyzer_Description), Resources.ResourceManager, typeof(Resources));
        internal const string Category = SupportedCategories.Usage;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ValidateColor.ToDiagnosticId(),
            Title,
            Message,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ValidateColor));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeFormatInvocation, SyntaxKind.InvocationExpression);

        private static void AnalyzeFormatInvocation(SyntaxNodeAnalysisContext context) =>
               AnalyzeFormatInvocation(context, "FromHtml");

        private static void AnalyzeFormatInvocation(SyntaxNodeAnalysisContext context, string methodName)
        {
            if (context.IsGenerated()) return;

            var invocation = (InvocationExpressionSyntax)context.Node;
            var memberExpresion = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberExpresion?.Name?.ToString() != methodName) return;
            if (!memberExpresion.DescendantTokens().Any(s => s.ValueText == nameof(ColorTranslator))) return;
            var argumentList = invocation.ArgumentList as ArgumentListSyntax;
            if (argumentList?.Arguments.Count != 1) return;
            var argument = argumentList.Arguments.First();
            if (argument.Expression.IsNotKind(SyntaxKind.StringLiteralExpression)) return;
            var htmlColor = ((LiteralExpressionSyntax)argument.Expression).Token.ValueText;
            try
            {
                ColorTranslator.FromHtml(htmlColor);
            }
            catch (Exception)
            {
                var diagnostic = Diagnostic.Create(Rule, memberExpresion.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        public enum KnownColor
        {
            ActiveBorder = 1,
            ActiveCaption = 2,
            ActiveCaptionText = 3,
            AppWorkspace = 4,
            Control = 5,
            ControlDark = 6,
            ControlDarkDark = 7,
            ControlLight = 8,
            ControlLightLight = 9,
            ControlText = 10,
            Desktop = 11,
            GrayText = 12,
            Highlight = 13,
            HighlightText = 14,
            HotTrack = 15,
            InactiveBorder = 16,
            InactiveCaption = 17,
            InactiveCaptionText = 18,
            Info = 19,
            InfoText = 20,
            Menu = 21,
            MenuText = 22,
            ScrollBar = 23,
            Window = 24,
            WindowFrame = 25,
            WindowText = 26,
            Transparent = 27,
            AliceBlue = 28,
            AntiqueWhite = 29,
            Aqua = 30,
            Aquamarine = 31,
            Azure = 32,
            Beige = 33,
            Bisque = 34,
            Black = 35,
            BlanchedAlmond = 36,
            Blue = 37,
            BlueViolet = 38,
            Brown = 39,
            BurlyWood = 40,
            CadetBlue = 41,
            Chartreuse = 42,
            Chocolate = 43,
            Coral = 44,
            CornflowerBlue = 45,
            Cornsilk = 46,
            Crimson = 47,
            Cyan = 48,
            DarkBlue = 49,
            DarkCyan = 50,
            DarkGoldenrod = 51,
            DarkGray = 52,
            DarkGreen = 53,
            DarkKhaki = 54,
            DarkMagenta = 55,
            DarkOliveGreen = 56,
            DarkOrange = 57,
            DarkOrchid = 58,
            DarkRed = 59,
            DarkSalmon = 60,
            DarkSeaGreen = 61,
            DarkSlateBlue = 62,
            DarkSlateGray = 63,
            DarkTurquoise = 64,
            DarkViolet = 65,
            DeepPink = 66,
            DeepSkyBlue = 67,
            DimGray = 68,
            DodgerBlue = 69,
            Firebrick = 70,
            FloralWhite = 71,
            ForestGreen = 72,
            Fuchsia = 73,
            Gainsboro = 74,
            GhostWhite = 75,
            Gold = 76,
            Goldenrod = 77,
            Gray = 78,
            Green = 79,
            GreenYellow = 80,
            Honeydew = 81,
            HotPink = 82,
            IndianRed = 83,
            Indigo = 84,
            Ivory = 85,
            Khaki = 86,
            Lavender = 87,
            LavenderBlush = 88,
            LawnGreen = 89,
            LemonChiffon = 90,
            LightBlue = 91,
            LightCoral = 92,
            LightCyan = 93,
            LightGoldenrodYellow = 94,
            LightGray = 95,
            LightGreen = 96,
            LightPink = 97,
            LightSalmon = 98,
            LightSeaGreen = 99,
            LightSkyBlue = 100,
            LightSlateGray = 101,
            LightSteelBlue = 102,
            LightYellow = 103,
            Lime = 104,
            LimeGreen = 105,
            Linen = 106,
            Magenta = 107,
            Maroon = 108,
            MediumAquamarine = 109,
            MediumBlue = 110,
            MediumOrchid = 111,
            MediumPurple = 112,
            MediumSeaGreen = 113,
            MediumSlateBlue = 114,
            MediumSpringGreen = 115,
            MediumTurquoise = 116,
            MediumVioletRed = 117,
            MidnightBlue = 118,
            MintCream = 119,
            MistyRose = 120,
            Moccasin = 121,
            NavajoWhite = 122,
            Navy = 123,
            OldLace = 124,
            Olive = 125,
            OliveDrab = 126,
            Orange = 127,
            OrangeRed = 128,
            Orchid = 129,
            PaleGoldenrod = 130,
            PaleGreen = 131,
            PaleTurquoise = 132,
            PaleVioletRed = 133,
            PapayaWhip = 134,
            PeachPuff = 135,
            Peru = 136,
            Pink = 137,
            Plum = 138,
            PowderBlue = 139,
            Purple = 140,
            Red = 141,
            RosyBrown = 142,
            RoyalBlue = 143,
            SaddleBrown = 144,
            Salmon = 145,
            SandyBrown = 146,
            SeaGreen = 147,
            SeaShell = 148,
            Sienna = 149,
            Silver = 150,
            SkyBlue = 151,
            SlateBlue = 152,
            SlateGray = 153,
            Snow = 154,
            SpringGreen = 155,
            SteelBlue = 156,
            Tan = 157,
            Teal = 158,
            Thistle = 159,
            Tomato = 160,
            Turquoise = 161,
            Violet = 162,
            Wheat = 163,
            White = 164,
            WhiteSmoke = 165,
            Yellow = 166,
            YellowGreen = 167,
            ButtonFace = 168,
            ButtonHighlight = 169,
            ButtonShadow = 170,
            GradientActiveCaption = 171,
            GradientInactiveCaption = 172,
            MenuBar = 173,
            MenuHighlight = 174
        }

        private struct Color
        {
            int value;

            public Color(KnownColor knownColor)
            {
                var color = FromKnownColor(knownColor);
                this.value = color.value;
            }

            public static Color FromArgb(int red, int green, int blue)
            {
                return FromArgb(255, red, green, blue);
            }

            public static Color FromArgb(int alpha, int red, int green, int blue)
            {
                if ((red > 255) || (red < 0))
                    throw CreateColorArgumentException(red, nameof(red));
                if ((green > 255) || (green < 0))
                    throw CreateColorArgumentException(green, nameof(green));
                if ((blue > 255) || (blue < 0))
                    throw CreateColorArgumentException(blue, nameof(blue));
                if ((alpha > 255) || (alpha < 0))
                    throw CreateColorArgumentException(alpha, nameof(alpha));

                var color = new Color
                {
                    value = (int)((uint)alpha << 24) + (red << 16) + (green << 8) + blue
                };
                return color;
            }

            public static Color FromArgb(int argb)
            {
                return FromArgb((argb >> 24) & 0x0FF, (argb >> 16) & 0x0FF, (argb >> 8) & 0x0FF, argb & 0x0FF);
            }

            public static Color FromKnownColor(KnownColor color)
            {
                return KnownColors.FromKnownColor(color);
            }

            public static readonly Color Empty = default(Color);

            public bool IsEmpty
            {
                get
                {
                    return value == 0;
                }
            }

            private static ArgumentException CreateColorArgumentException(int value, string color)
            {
                return new ArgumentException(string.Format("'{0}' is not a valid"
                    + " value for '{1}'. '{1}' should be greater or equal to 0 and"
                    + " less than or equal to 255.", value, color));
            }

            static public Color LightGray
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightGray); }
            }
        }

        private class ColorConverter
        {
            public static Color ConvertFromString(string s, CultureInfo culture)
            {
                s = s.Trim();

                // Try to process both NamedColor and SystemColors from the KnownColor enumeration
                if (Char.IsLetter(s[0]))
                {
                    KnownColor kc;
                    try
                    {
                        kc = (KnownColor)Enum.Parse(typeof(KnownColor), s, true);
                    }
                    catch (Exception e)
                    {
                        // whatever happens MS throws an basic Exception
                        var msg = String.Format("Invalid color name '{0}'.", s);
                        throw new Exception(msg, new FormatException(msg, e));
                    }
                    return KnownColors.FromKnownColor(kc);
                }

                var numSeparator = culture.TextInfo.ListSeparator;
                var result = Color.Empty;

                if (s.IndexOf(numSeparator) == -1)
                {
                    var sharp = (s[0] == '#');
                    var start = sharp ? 1 : 0;
                    var hex = false;
                    // deal with #hex, 0xhex and #0xhex
                    if ((s.Length > start + 1) && (s[start] == '0'))
                    {
                        hex = ((s[start + 1] == 'x') || (s[start + 1] == 'X'));
                        if (hex)
                            start += 2;
                    }

                    if (sharp || hex)
                    {
                        s = s.Substring(start);
                        int argb;
                        try
                        {
                            argb = Int32.Parse(s, NumberStyles.HexNumber);
                        }
                        catch (Exception e)
                        {
                            // whatever happens MS throws an basic Exception
                            var msg = String.Format("Invalid Int32 value '{0}'.", s);
                            throw new Exception(msg, e);
                        }

                        // note that the default alpha value for a 6 hex digit (i.e. when none are present) is 
                        // 0xFF while shorter string defaults to 0xFF - unless both # an 0x are specified
                        if ((s.Length < 6) || ((s.Length == 6) && sharp && hex))
                            argb &= 0x00FFFFFF;
                        else if ((argb >> 24) == 0)
                            argb |= unchecked((int)0xFF000000);
                        result = Color.FromArgb(argb);
                    }
                }

                if (result.IsEmpty)
                {
                    var components = s.Split(numSeparator.ToCharArray());

                    // MS seems to convert the indivual component to int before
                    // checking the number of components
                    var numComponents = new int[components.Length];
                    for (int i = 0; i < numComponents.Length; i++)
                    {
                        numComponents[i] = Int32.Parse(components[i]);
                    }

                    switch (components.Length)
                    {
                        case 1:
                            result = Color.FromArgb(numComponents[0]);
                            break;
                        case 3:
                            result = Color.FromArgb(numComponents[0], numComponents[1],
                                numComponents[2]);
                            break;
                        case 4:
                            result = Color.FromArgb(numComponents[0], numComponents[1],
                                numComponents[2], numComponents[3]);
                            break;
                        default:
                            throw new ArgumentException(s + " is not a valid color value.");
                    }
                }

                return result;
            }
        }

        private sealed class ColorTranslator
        {
            public static Color FromHtml(string htmlColor)
            {
                if ((htmlColor == null) || (htmlColor.Length == 0))
                    return Color.Empty;

                switch (htmlColor.ToLowerInvariant())
                {
                    case "buttonface":
                    case "threedface":
                        return SystemColors.Control;
                    case "buttonhighlight":
                    case "threedlightshadow":
                        return SystemColors.ControlLightLight;
                    case "buttonshadow":
                        return SystemColors.ControlDark;
                    case "captiontext":
                        return SystemColors.ActiveCaptionText;
                    case "threeddarkshadow":
                        return SystemColors.ControlDarkDark;
                    case "threedhighlight":
                        return SystemColors.ControlLight;
                    case "background":
                        return SystemColors.Desktop;
                    case "buttontext":
                        return SystemColors.ControlText;
                    case "infobackground":
                        return SystemColors.Info;
                    // special case for Color.LightGray versus html's LightGrey (#340917)
                    case "lightgrey":
                        return Color.LightGray;
                    default:
                        return ColorConverter.ConvertFromString(htmlColor, CultureInfo.CurrentCulture);
                }
            }
        }

        private static class KnownColors
        {
            static internal uint[] ArgbValues = new uint[] {
                0x00000000, /* 000 - Empty */
                0xFFD4D0C8, /* 001 - ActiveBorder */
                0xFF0054E3, /* 002 - ActiveCaption */
                0xFFFFFFFF, /* 003 - ActiveCaptionText */
                0xFF808080, /* 004 - AppWorkspace */
                0xFFECE9D8, /* 005 - Control */
                0xFFACA899, /* 006 - ControlDark */
                0xFF716F64, /* 007 - ControlDarkDark */
                0xFFF1EFE2, /* 008 - ControlLight */
                0xFFFFFFFF, /* 009 - ControlLightLight */
                0xFF000000, /* 010 - ControlText */
                0xFF004E98, /* 011 - Desktop */
                0xFFACA899, /* 012 - GrayText */
                0xFF316AC5, /* 013 - Highlight */
                0xFFFFFFFF, /* 014 - HighlightText */
                0xFF000080, /* 015 - HotTrack */
                0xFFD4D0C8, /* 016 - InactiveBorder */
                0xFF7A96DF, /* 017 - InactiveCaption */
                0xFFD8E4F8, /* 018 - InactiveCaptionText */
                0xFFFFFFE1, /* 019 - Info */
                0xFF000000, /* 020 - InfoText */
                0xFFFFFFFF, /* 021 - Menu */
                0xFF000000, /* 022 - MenuText */
                0xFFD4D0C8, /* 023 - ScrollBar */
                0xFFFFFFFF, /* 024 - Window */
                0xFF000000, /* 025 - WindowFrame */
                0xFF000000, /* 026 - WindowText */
                0x00FFFFFF, /* 027 - Transparent */
                0xFFF0F8FF, /* 028 - AliceBlue */
                0xFFFAEBD7, /* 029 - AntiqueWhite */
                0xFF00FFFF, /* 030 - Aqua */
                0xFF7FFFD4, /* 031 - Aquamarine */
                0xFFF0FFFF, /* 032 - Azure */
                0xFFF5F5DC, /* 033 - Beige */
                0xFFFFE4C4, /* 034 - Bisque */
                0xFF000000, /* 035 - Black */
                0xFFFFEBCD, /* 036 - BlanchedAlmond */
                0xFF0000FF, /* 037 - Blue */
                0xFF8A2BE2, /* 038 - BlueViolet */
                0xFFA52A2A, /* 039 - Brown */
                0xFFDEB887, /* 040 - BurlyWood */
                0xFF5F9EA0, /* 041 - CadetBlue */
                0xFF7FFF00, /* 042 - Chartreuse */
                0xFFD2691E, /* 043 - Chocolate */
                0xFFFF7F50, /* 044 - Coral */
                0xFF6495ED, /* 045 - CornflowerBlue */
                0xFFFFF8DC, /* 046 - Cornsilk */
                0xFFDC143C, /* 047 - Crimson */
                0xFF00FFFF, /* 048 - Cyan */
                0xFF00008B, /* 049 - DarkBlue */
                0xFF008B8B, /* 050 - DarkCyan */
                0xFFB8860B, /* 051 - DarkGoldenrod */
                0xFFA9A9A9, /* 052 - DarkGray */
                0xFF006400, /* 053 - DarkGreen */
                0xFFBDB76B, /* 054 - DarkKhaki */
                0xFF8B008B, /* 055 - DarkMagenta */
                0xFF556B2F, /* 056 - DarkOliveGreen */
                0xFFFF8C00, /* 057 - DarkOrange */
                0xFF9932CC, /* 058 - DarkOrchid */
                0xFF8B0000, /* 059 - DarkRed */
                0xFFE9967A, /* 060 - DarkSalmon */
                0xFF8FBC8B, /* 061 - DarkSeaGreen */
                0xFF483D8B, /* 062 - DarkSlateBlue */
                0xFF2F4F4F, /* 063 - DarkSlateGray */
                0xFF00CED1, /* 064 - DarkTurquoise */
                0xFF9400D3, /* 065 - DarkViolet */
                0xFFFF1493, /* 066 - DeepPink */
                0xFF00BFFF, /* 067 - DeepSkyBlue */
                0xFF696969, /* 068 - DimGray */
                0xFF1E90FF, /* 069 - DodgerBlue */
                0xFFB22222, /* 070 - Firebrick */
                0xFFFFFAF0, /* 071 - FloralWhite */
                0xFF228B22, /* 072 - ForestGreen */
                0xFFFF00FF, /* 073 - Fuchsia */
                0xFFDCDCDC, /* 074 - Gainsboro */
                0xFFF8F8FF, /* 075 - GhostWhite */
                0xFFFFD700, /* 076 - Gold */
                0xFFDAA520, /* 077 - Goldenrod */
                0xFF808080, /* 078 - Gray */
                0xFF008000, /* 079 - Green */
                0xFFADFF2F, /* 080 - GreenYellow */
                0xFFF0FFF0, /* 081 - Honeydew */
                0xFFFF69B4, /* 082 - HotPink */
                0xFFCD5C5C, /* 083 - IndianRed */
                0xFF4B0082, /* 084 - Indigo */
                0xFFFFFFF0, /* 085 - Ivory */
                0xFFF0E68C, /* 086 - Khaki */
                0xFFE6E6FA, /* 087 - Lavender */
                0xFFFFF0F5, /* 088 - LavenderBlush */
                0xFF7CFC00, /* 089 - LawnGreen */
                0xFFFFFACD, /* 090 - LemonChiffon */
                0xFFADD8E6, /* 091 - LightBlue */
                0xFFF08080, /* 092 - LightCoral */
                0xFFE0FFFF, /* 093 - LightCyan */
                0xFFFAFAD2, /* 094 - LightGoldenrodYellow */
                0xFFD3D3D3, /* 095 - LightGray */
                0xFF90EE90, /* 096 - LightGreen */
                0xFFFFB6C1, /* 097 - LightPink */
                0xFFFFA07A, /* 098 - LightSalmon */
                0xFF20B2AA, /* 099 - LightSeaGreen */
                0xFF87CEFA, /* 100 - LightSkyBlue */
                0xFF778899, /* 101 - LightSlateGray */
                0xFFB0C4DE, /* 102 - LightSteelBlue */
                0xFFFFFFE0, /* 103 - LightYellow */
                0xFF00FF00, /* 104 - Lime */
                0xFF32CD32, /* 105 - LimeGreen */
                0xFFFAF0E6, /* 106 - Linen */
                0xFFFF00FF, /* 107 - Magenta */
                0xFF800000, /* 108 - Maroon */
                0xFF66CDAA, /* 109 - MediumAquamarine */
                0xFF0000CD, /* 110 - MediumBlue */
                0xFFBA55D3, /* 111 - MediumOrchid */
                0xFF9370DB, /* 112 - MediumPurple */
                0xFF3CB371, /* 113 - MediumSeaGreen */
                0xFF7B68EE, /* 114 - MediumSlateBlue */
                0xFF00FA9A, /* 115 - MediumSpringGreen */
                0xFF48D1CC, /* 116 - MediumTurquoise */
                0xFFC71585, /* 117 - MediumVioletRed */
                0xFF191970, /* 118 - MidnightBlue */
                0xFFF5FFFA, /* 119 - MintCream */
                0xFFFFE4E1, /* 120 - MistyRose */
                0xFFFFE4B5, /* 121 - Moccasin */
                0xFFFFDEAD, /* 122 - NavajoWhite */
                0xFF000080, /* 123 - Navy */
                0xFFFDF5E6, /* 124 - OldLace */
                0xFF808000, /* 125 - Olive */
                0xFF6B8E23, /* 126 - OliveDrab */
                0xFFFFA500, /* 127 - Orange */
                0xFFFF4500, /* 128 - OrangeRed */
                0xFFDA70D6, /* 129 - Orchid */
                0xFFEEE8AA, /* 130 - PaleGoldenrod */
                0xFF98FB98, /* 131 - PaleGreen */
                0xFFAFEEEE, /* 132 - PaleTurquoise */
                0xFFDB7093, /* 133 - PaleVioletRed */
                0xFFFFEFD5, /* 134 - PapayaWhip */
                0xFFFFDAB9, /* 135 - PeachPuff */
                0xFFCD853F, /* 136 - Peru */
                0xFFFFC0CB, /* 137 - Pink */
                0xFFDDA0DD, /* 138 - Plum */
                0xFFB0E0E6, /* 139 - PowderBlue */
                0xFF800080, /* 140 - Purple */
                0xFFFF0000, /* 141 - Red */
                0xFFBC8F8F, /* 142 - RosyBrown */
                0xFF4169E1, /* 143 - RoyalBlue */
                0xFF8B4513, /* 144 - SaddleBrown */
                0xFFFA8072, /* 145 - Salmon */
                0xFFF4A460, /* 146 - SandyBrown */
                0xFF2E8B57, /* 147 - SeaGreen */
                0xFFFFF5EE, /* 148 - SeaShell */
                0xFFA0522D, /* 149 - Sienna */
                0xFFC0C0C0, /* 150 - Silver */
                0xFF87CEEB, /* 151 - SkyBlue */
                0xFF6A5ACD, /* 152 - SlateBlue */
                0xFF708090, /* 153 - SlateGray */
                0xFFFFFAFA, /* 154 - Snow */
                0xFF00FF7F, /* 155 - SpringGreen */
                0xFF4682B4, /* 156 - SteelBlue */
                0xFFD2B48C, /* 157 - Tan */
                0xFF008080, /* 158 - Teal */
                0xFFD8BFD8, /* 159 - Thistle */
                0xFFFF6347, /* 160 - Tomato */
                0xFF40E0D0, /* 161 - Turquoise */
                0xFFEE82EE, /* 162 - Violet */
                0xFFF5DEB3, /* 163 - Wheat */
                0xFFFFFFFF, /* 164 - White */
                0xFFF5F5F5, /* 165 - WhiteSmoke */
                0xFFFFFF00, /* 166 - Yellow */
                0xFF9ACD32, /* 167 - YellowGreen */
                0xFFECE9D8, /* 168 - ButtonFace */
                0xFFFFFFFF, /* 169 - ButtonHighlight */
                0xFFACA899, /* 170 - ButtonShadow */
                0xFF3D95FF, /* 171 - GradientActiveCaption */
                0xFF9DB9EB, /* 172 - GradientInactiveCaption */
                0xFFECE9D8, /* 173 - MenuBar */
                0xFF316AC5, /* 174 - MenuHighlight */
            };

            static KnownColors()
            {
                // note: Mono's SWF Theme class will call the static Update method to apply
                // correct system colors outside Windows
            }

            public static Color FromKnownColor(KnownColor kc)
            {
                Color c;
                var n = (short)kc;
                c = (n <= 0) || (n >= ArgbValues.Length) ? Color.FromArgb(0) : Color.FromArgb((int)ArgbValues[n]);
                return c;
            }
        }

        private sealed class SystemColors
        {

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.ActiveCaptionText"]/*' />
            /// <devdoc>
            ///     The color of the text of an active title bar caption.
            /// </devdoc>
            public static Color ActiveCaptionText
            {
                get
                {
                    return new Color(KnownColor.ActiveCaptionText);
                }
            }


            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.Control"]/*' />
            /// <devdoc>
            ///     The color of the background of push buttons and other 3D objects.
            /// </devdoc>
            public static Color Control
            {
                get
                {
                    return new Color(KnownColor.Control);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.ControlDark"]/*' />
            /// <devdoc>
            ///     The color of shadows on 3D objects.
            /// </devdoc>
            public static Color ControlDark
            {
                get
                {
                    return new Color(KnownColor.ControlDark);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.ControlDarkDark"]/*' />
            /// <devdoc>
            ///     The color of very dark shadows on 3D objects.
            /// </devdoc>
            public static Color ControlDarkDark
            {
                get
                {
                    return new Color(KnownColor.ControlDarkDark);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.ControlLight"]/*' />
            /// <devdoc>
            ///     The color of highlights on 3D objects.
            /// </devdoc>
            public static Color ControlLight
            {
                get
                {
                    return new Color(KnownColor.ControlLight);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.ControlLightLight"]/*' />
            /// <devdoc>
            ///     The color of very light highlights on 3D objects.
            /// </devdoc>
            public static Color ControlLightLight
            {
                get
                {
                    return new Color(KnownColor.ControlLightLight);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.ControlText"]/*' />
            /// <devdoc>
            ///     The color of the text of push buttons and other 3D objects
            /// </devdoc>
            public static Color ControlText
            {
                get
                {
                    return new Color(KnownColor.ControlText);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.Desktop"]/*' />
            /// <devdoc>
            ///     This color is the user-defined color of the Windows desktop.
            /// </devdoc>
            public static Color Desktop
            {
                get
                {
                    return new Color(KnownColor.Desktop);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.Info"]/*' />
            /// <devdoc>
            ///     The color of the info/tool tip background.
            /// </devdoc>
            public static Color Info
            {
                get
                {
                    return new Color(KnownColor.Info);
                }
            }
        }
    }

}