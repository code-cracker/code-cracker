using CodeCracker.CSharp.Usage.MethodAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Globalization;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ValidateColorAnalyzer : DiagnosticAnalyzer
    {
        internal const string Category = SupportedCategories.Usage;
        internal const string Message = "Your htmlColor value doesn't exist.";
        internal const string Title = "Color validation";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ValidateColor.ToDiagnosticId(),
            Title,
            Message,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ValidateColor));
        const string Description = @"This diagnostic checks the htmlColor value and triggers if the parsing fail by throwing an exception.";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.InvocationExpression);


        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var memberExpresion = invocationExpression.Expression as MemberAccessExpressionSyntax;
            if (memberExpresion?.Name?.ToString() != "FromHtml") return;

            if (memberExpresion.Expression as IdentifierNameSyntax != null)
            {
                if (((IdentifierNameSyntax)memberExpresion.Expression).Identifier.Text != nameof(ColorTranslator)) return;
            }
            else
            {
                if (((IdentifierNameSyntax)((MemberAccessExpressionSyntax)memberExpresion.Expression).Name).Identifier.Text != nameof(ColorTranslator)) return;
            }

            var argumentList = invocationExpression.ArgumentList as ArgumentListSyntax;
            if (argumentList?.Arguments.Count != 1) return;
            var argument = argumentList.Arguments.First();
            if (argument.Expression as LiteralExpressionSyntax == null) return;
            var htmlColor = ((LiteralExpressionSyntax)argument.Expression).Token.ValueText;
            try
            {
                var color = ColorTranslator.FromHtml(htmlColor);
            }
            catch (Exception)
            {
                var diagnostic = Diagnostic.Create(Rule, memberExpresion.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        internal enum KnownColor
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

        internal struct Color
        {
            int value;

            #region Unimplemented bloated properties
            //
            // These properties were implemented very poorly on Mono, this
            // version will only store the int32 value and any helper properties
            // like Name, IsKnownColor, IsSystemColor, IsNamedColor are not
            // currently implemented, and would be implemented in the future
            // using external tables/hastables/dictionaries, without bloating
            // the Color structure
            //
            public string Name
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsKnownColor
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsSystemColor
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsNamedColor
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
            #endregion

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

            public int ToArgb()
            {
                return (int)value;
            }

            public static Color FromArgb(int alpha, Color baseColor)
            {
                return FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
            }

            public static Color FromArgb(int argb)
            {
                return FromArgb((argb >> 24) & 0x0FF, (argb >> 16) & 0x0FF, (argb >> 8) & 0x0FF, argb & 0x0FF);
            }

            public static Color FromKnownColor(KnownColor color)
            {
                return KnownColors.FromKnownColor(color);
            }

            public static Color FromName(string name)
            {
                try
                {
                    var kc = (KnownColor)Enum.Parse(typeof(KnownColor), name, true);
                    return KnownColors.FromKnownColor(kc);
                }
                catch (Exception)
                {
                    // This is what it returns! 	 
                    var d = FromArgb(0, 0, 0, 0);
                    return d;
                }
            }


            public static readonly Color Empty;

            public static bool operator ==(Color left, Color right)
            {
                return left.value == right.value;
            }

            public static bool operator !=(Color left, Color right)
            {
                return left.value != right.value;
            }

            public float GetBrightness()
            {
                var minval = Math.Min(R, Math.Min(G, B));
                var maxval = Math.Max(R, Math.Max(G, B));

                return (float)(maxval + minval) / 510;
            }

            public float GetSaturation()
            {
                var minval = (byte)Math.Min(R, Math.Min(G, B));
                var maxval = (byte)Math.Max(R, Math.Max(G, B));

                if (maxval == minval)
                    return 0.0f;

                var sum = maxval + minval;
                if (sum > 255)
                    sum = 510 - sum;

                return (float)(maxval - minval) / sum;
            }

            public float GetHue()
            {
                int r = R;
                int g = G;
                int b = B;
                var minval = (byte)Math.Min(r, Math.Min(g, b));
                var maxval = (byte)Math.Max(r, Math.Max(g, b));

                if (maxval == minval)
                    return 0.0f;

                var diff = (float)(maxval - minval);
                var rnorm = (maxval - r) / diff;
                var gnorm = (maxval - g) / diff;
                var bnorm = (maxval - b) / diff;

                var hue = 0.0f;
                if (r == maxval)
                    hue = 60.0f * (6.0f + bnorm - gnorm);
                if (g == maxval)
                    hue = 60.0f * (2.0f + rnorm - bnorm);
                if (b == maxval)
                    hue = 60.0f * (4.0f + gnorm - rnorm);
                if (hue > 360.0f)
                    hue = hue - 360.0f;

                return hue;
            }

            public static KnownColor ToKnownColor()
            {
                throw new NotImplementedException();
            }

            public bool IsEmpty
            {
                get
                {
                    return value == 0;
                }
            }

            public byte A
            {
                get { return (byte)(value >> 24); }
            }

            public byte R
            {
                get { return (byte)(value >> 16); }
            }

            public byte G
            {
                get { return (byte)(value >> 8); }
            }

            public byte B
            {
                get { return (byte)value; }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Color))
                    return false;
                var c = (Color)obj;
                return this == c;
            }

            public override int GetHashCode()
            {
                return value;
            }

            public override string ToString()
            {
                if (IsEmpty)
                    return "Color [Empty]";

                return String.Format("Color [A={0}, R={1}, G={2}, B={3}]", A, R, G, B);
            }

            private static ArgumentException CreateColorArgumentException(int value, string color)
            {
                return new ArgumentException(string.Format("'{0}' is not a valid"
                    + " value for '{1}'. '{1}' should be greater or equal to 0 and"
                    + " less than or equal to 255.", value, color));
            }

            static public Color Transparent
            {
                get { return KnownColors.FromKnownColor(KnownColor.Transparent); }
            }

            static public Color AliceBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.AliceBlue); }
            }

            static public Color AntiqueWhite
            {
                get { return KnownColors.FromKnownColor(KnownColor.AntiqueWhite); }
            }

            static public Color Aqua
            {
                get { return KnownColors.FromKnownColor(KnownColor.Aqua); }
            }

            static public Color Aquamarine
            {
                get { return KnownColors.FromKnownColor(KnownColor.Aquamarine); }
            }

            static public Color Azure
            {
                get { return KnownColors.FromKnownColor(KnownColor.Azure); }
            }

            static public Color Beige
            {
                get { return KnownColors.FromKnownColor(KnownColor.Beige); }
            }

            static public Color Bisque
            {
                get { return KnownColors.FromKnownColor(KnownColor.Bisque); }
            }

            static public Color Black
            {
                get { return KnownColors.FromKnownColor(KnownColor.Black); }
            }

            static public Color BlanchedAlmond
            {
                get { return KnownColors.FromKnownColor(KnownColor.BlanchedAlmond); }
            }

            static public Color Blue
            {
                get { return KnownColors.FromKnownColor(KnownColor.Blue); }
            }

            static public Color BlueViolet
            {
                get { return KnownColors.FromKnownColor(KnownColor.BlueViolet); }
            }

            static public Color Brown
            {
                get { return KnownColors.FromKnownColor(KnownColor.Brown); }
            }

            static public Color BurlyWood
            {
                get { return KnownColors.FromKnownColor(KnownColor.BurlyWood); }
            }

            static public Color CadetBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.CadetBlue); }
            }

            static public Color Chartreuse
            {
                get { return KnownColors.FromKnownColor(KnownColor.Chartreuse); }
            }

            static public Color Chocolate
            {
                get { return KnownColors.FromKnownColor(KnownColor.Chocolate); }
            }

            static public Color Coral
            {
                get { return KnownColors.FromKnownColor(KnownColor.Coral); }
            }

            static public Color CornflowerBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.CornflowerBlue); }
            }

            static public Color Cornsilk
            {
                get { return KnownColors.FromKnownColor(KnownColor.Cornsilk); }
            }

            static public Color Crimson
            {
                get { return KnownColors.FromKnownColor(KnownColor.Crimson); }
            }

            static public Color Cyan
            {
                get { return KnownColors.FromKnownColor(KnownColor.Cyan); }
            }

            static public Color DarkBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkBlue); }
            }

            static public Color DarkCyan
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkCyan); }
            }

            static public Color DarkGoldenrod
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkGoldenrod); }
            }

            static public Color DarkGray
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkGray); }
            }

            static public Color DarkGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkGreen); }
            }

            static public Color DarkKhaki
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkKhaki); }
            }

            static public Color DarkMagenta
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkMagenta); }
            }

            static public Color DarkOliveGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkOliveGreen); }
            }

            static public Color DarkOrange
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkOrange); }
            }

            static public Color DarkOrchid
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkOrchid); }
            }

            static public Color DarkRed
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkRed); }
            }

            static public Color DarkSalmon
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkSalmon); }
            }

            static public Color DarkSeaGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkSeaGreen); }
            }

            static public Color DarkSlateBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkSlateBlue); }
            }

            static public Color DarkSlateGray
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkSlateGray); }
            }

            static public Color DarkTurquoise
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkTurquoise); }
            }

            static public Color DarkViolet
            {
                get { return KnownColors.FromKnownColor(KnownColor.DarkViolet); }
            }

            static public Color DeepPink
            {
                get { return KnownColors.FromKnownColor(KnownColor.DeepPink); }
            }

            static public Color DeepSkyBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.DeepSkyBlue); }
            }

            static public Color DimGray
            {
                get { return KnownColors.FromKnownColor(KnownColor.DimGray); }
            }

            static public Color DodgerBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.DodgerBlue); }
            }

            static public Color Firebrick
            {
                get { return KnownColors.FromKnownColor(KnownColor.Firebrick); }
            }

            static public Color FloralWhite
            {
                get { return KnownColors.FromKnownColor(KnownColor.FloralWhite); }
            }

            static public Color ForestGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.ForestGreen); }
            }

            static public Color Fuchsia
            {
                get { return KnownColors.FromKnownColor(KnownColor.Fuchsia); }
            }

            static public Color Gainsboro
            {
                get { return KnownColors.FromKnownColor(KnownColor.Gainsboro); }
            }

            static public Color GhostWhite
            {
                get { return KnownColors.FromKnownColor(KnownColor.GhostWhite); }
            }

            static public Color Gold
            {
                get { return KnownColors.FromKnownColor(KnownColor.Gold); }
            }

            static public Color Goldenrod
            {
                get { return KnownColors.FromKnownColor(KnownColor.Goldenrod); }
            }

            static public Color Gray
            {
                get { return KnownColors.FromKnownColor(KnownColor.Gray); }
            }

            static public Color Green
            {
                get { return KnownColors.FromKnownColor(KnownColor.Green); }
            }

            static public Color GreenYellow
            {
                get { return KnownColors.FromKnownColor(KnownColor.GreenYellow); }
            }

            static public Color Honeydew
            {
                get { return KnownColors.FromKnownColor(KnownColor.Honeydew); }
            }

            static public Color HotPink
            {
                get { return KnownColors.FromKnownColor(KnownColor.HotPink); }
            }

            static public Color IndianRed
            {
                get { return KnownColors.FromKnownColor(KnownColor.IndianRed); }
            }

            static public Color Indigo
            {
                get { return KnownColors.FromKnownColor(KnownColor.Indigo); }
            }

            static public Color Ivory
            {
                get { return KnownColors.FromKnownColor(KnownColor.Ivory); }
            }

            static public Color Khaki
            {
                get { return KnownColors.FromKnownColor(KnownColor.Khaki); }
            }

            static public Color Lavender
            {
                get { return KnownColors.FromKnownColor(KnownColor.Lavender); }
            }

            static public Color LavenderBlush
            {
                get { return KnownColors.FromKnownColor(KnownColor.LavenderBlush); }
            }

            static public Color LawnGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.LawnGreen); }
            }

            static public Color LemonChiffon
            {
                get { return KnownColors.FromKnownColor(KnownColor.LemonChiffon); }
            }

            static public Color LightBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightBlue); }
            }

            static public Color LightCoral
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightCoral); }
            }

            static public Color LightCyan
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightCyan); }
            }

            static public Color LightGoldenrodYellow
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightGoldenrodYellow); }
            }

            static public Color LightGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightGreen); }
            }

            static public Color LightGray
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightGray); }
            }

            static public Color LightPink
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightPink); }
            }

            static public Color LightSalmon
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightSalmon); }
            }

            static public Color LightSeaGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightSeaGreen); }
            }

            static public Color LightSkyBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightSkyBlue); }
            }

            static public Color LightSlateGray
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightSlateGray); }
            }

            static public Color LightSteelBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightSteelBlue); }
            }

            static public Color LightYellow
            {
                get { return KnownColors.FromKnownColor(KnownColor.LightYellow); }
            }

            static public Color Lime
            {
                get { return KnownColors.FromKnownColor(KnownColor.Lime); }
            }

            static public Color LimeGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.LimeGreen); }
            }

            static public Color Linen
            {
                get { return KnownColors.FromKnownColor(KnownColor.Linen); }
            }

            static public Color Magenta
            {
                get { return KnownColors.FromKnownColor(KnownColor.Magenta); }
            }

            static public Color Maroon
            {
                get { return KnownColors.FromKnownColor(KnownColor.Maroon); }
            }

            static public Color MediumAquamarine
            {
                get { return KnownColors.FromKnownColor(KnownColor.MediumAquamarine); }
            }

            static public Color MediumBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.MediumBlue); }
            }

            static public Color MediumOrchid
            {
                get { return KnownColors.FromKnownColor(KnownColor.MediumOrchid); }
            }

            static public Color MediumPurple
            {
                get { return KnownColors.FromKnownColor(KnownColor.MediumPurple); }
            }

            static public Color MediumSeaGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.MediumSeaGreen); }
            }

            static public Color MediumSlateBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.MediumSlateBlue); }
            }

            static public Color MediumSpringGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.MediumSpringGreen); }
            }

            static public Color MediumTurquoise
            {
                get { return KnownColors.FromKnownColor(KnownColor.MediumTurquoise); }
            }

            static public Color MediumVioletRed
            {
                get { return KnownColors.FromKnownColor(KnownColor.MediumVioletRed); }
            }

            static public Color MidnightBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.MidnightBlue); }
            }

            static public Color MintCream
            {
                get { return KnownColors.FromKnownColor(KnownColor.MintCream); }
            }

            static public Color MistyRose
            {
                get { return KnownColors.FromKnownColor(KnownColor.MistyRose); }
            }

            static public Color Moccasin
            {
                get { return KnownColors.FromKnownColor(KnownColor.Moccasin); }
            }

            static public Color NavajoWhite
            {
                get { return KnownColors.FromKnownColor(KnownColor.NavajoWhite); }
            }

            static public Color Navy
            {
                get { return KnownColors.FromKnownColor(KnownColor.Navy); }
            }

            static public Color OldLace
            {
                get { return KnownColors.FromKnownColor(KnownColor.OldLace); }
            }

            static public Color Olive
            {
                get { return KnownColors.FromKnownColor(KnownColor.Olive); }
            }

            static public Color OliveDrab
            {
                get { return KnownColors.FromKnownColor(KnownColor.OliveDrab); }
            }

            static public Color Orange
            {
                get { return KnownColors.FromKnownColor(KnownColor.Orange); }
            }

            static public Color OrangeRed
            {
                get { return KnownColors.FromKnownColor(KnownColor.OrangeRed); }
            }

            static public Color Orchid
            {
                get { return KnownColors.FromKnownColor(KnownColor.Orchid); }
            }

            static public Color PaleGoldenrod
            {
                get { return KnownColors.FromKnownColor(KnownColor.PaleGoldenrod); }
            }

            static public Color PaleGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.PaleGreen); }
            }

            static public Color PaleTurquoise
            {
                get { return KnownColors.FromKnownColor(KnownColor.PaleTurquoise); }
            }

            static public Color PaleVioletRed
            {
                get { return KnownColors.FromKnownColor(KnownColor.PaleVioletRed); }
            }

            static public Color PapayaWhip
            {
                get { return KnownColors.FromKnownColor(KnownColor.PapayaWhip); }
            }

            static public Color PeachPuff
            {
                get { return KnownColors.FromKnownColor(KnownColor.PeachPuff); }
            }

            static public Color Peru
            {
                get { return KnownColors.FromKnownColor(KnownColor.Peru); }
            }

            static public Color Pink
            {
                get { return KnownColors.FromKnownColor(KnownColor.Pink); }
            }

            static public Color Plum
            {
                get { return KnownColors.FromKnownColor(KnownColor.Plum); }
            }

            static public Color PowderBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.PowderBlue); }
            }

            static public Color Purple
            {
                get { return KnownColors.FromKnownColor(KnownColor.Purple); }
            }

            static public Color Red
            {
                get { return KnownColors.FromKnownColor(KnownColor.Red); }
            }

            static public Color RosyBrown
            {
                get { return KnownColors.FromKnownColor(KnownColor.RosyBrown); }
            }

            static public Color RoyalBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.RoyalBlue); }
            }

            static public Color SaddleBrown
            {
                get { return KnownColors.FromKnownColor(KnownColor.SaddleBrown); }
            }

            static public Color Salmon
            {
                get { return KnownColors.FromKnownColor(KnownColor.Salmon); }
            }

            static public Color SandyBrown
            {
                get { return KnownColors.FromKnownColor(KnownColor.SandyBrown); }
            }

            static public Color SeaGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.SeaGreen); }
            }

            static public Color SeaShell
            {
                get { return KnownColors.FromKnownColor(KnownColor.SeaShell); }
            }

            static public Color Sienna
            {
                get { return KnownColors.FromKnownColor(KnownColor.Sienna); }
            }

            static public Color Silver
            {
                get { return KnownColors.FromKnownColor(KnownColor.Silver); }
            }

            static public Color SkyBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.SkyBlue); }
            }

            static public Color SlateBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.SlateBlue); }
            }

            static public Color SlateGray
            {
                get { return KnownColors.FromKnownColor(KnownColor.SlateGray); }
            }

            static public Color Snow
            {
                get { return KnownColors.FromKnownColor(KnownColor.Snow); }
            }

            static public Color SpringGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.SpringGreen); }
            }

            static public Color SteelBlue
            {
                get { return KnownColors.FromKnownColor(KnownColor.SteelBlue); }
            }

            static public Color Tan
            {
                get { return KnownColors.FromKnownColor(KnownColor.Tan); }
            }

            static public Color Teal
            {
                get { return KnownColors.FromKnownColor(KnownColor.Teal); }
            }

            static public Color Thistle
            {
                get { return KnownColors.FromKnownColor(KnownColor.Thistle); }
            }

            static public Color Tomato
            {
                get { return KnownColors.FromKnownColor(KnownColor.Tomato); }
            }

            static public Color Turquoise
            {
                get { return KnownColors.FromKnownColor(KnownColor.Turquoise); }
            }

            static public Color Violet
            {
                get { return KnownColors.FromKnownColor(KnownColor.Violet); }
            }

            static public Color Wheat
            {
                get { return KnownColors.FromKnownColor(KnownColor.Wheat); }
            }

            static public Color White
            {
                get { return KnownColors.FromKnownColor(KnownColor.White); }
            }

            static public Color WhiteSmoke
            {
                get { return KnownColors.FromKnownColor(KnownColor.WhiteSmoke); }
            }

            static public Color Yellow
            {
                get { return KnownColors.FromKnownColor(KnownColor.Yellow); }
            }

            static public Color YellowGreen
            {
                get { return KnownColors.FromKnownColor(KnownColor.YellowGreen); }
            }
        }

        internal class ColorConverter
        {
            public ColorConverter() { }

            public static Color ConvertFromString(string s, CultureInfo culture)
            {
                if (culture == null)
                    culture = CultureInfo.InvariantCulture;

                s = s.Trim();

                if (s.Length == 0)
                    return Color.Empty;

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

                if (!result.IsEmpty)
                {
                    // Look for a named or system color with those values
                    var known = KnownColors.FindColorMatch(result);
                    if (!known.IsEmpty)
                        return known;
                }

                return result;
            }
        }

        internal sealed class ColorTranslator
        {

            private ColorTranslator()
            {
            }

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
                }

                return ColorConverter.ConvertFromString(htmlColor, CultureInfo.CurrentCulture);
            }

            internal static Color FromBGR(int bgr)
            {
                var result = Color.FromArgb(0xFF, (bgr & 0xFF), ((bgr >> 8) & 0xFF), ((bgr >> 16) & 0xFF));
                var known = KnownColors.FindColorMatch(result);
                return (known.IsEmpty) ? result : known;
            }

            public static Color FromOle(int oleColor)
            {
                // OleColor format is BGR
                return FromBGR(oleColor);
            }

            public static Color FromWin32(int win32Color)
            {
                // Win32Color format is BGR
                return FromBGR(win32Color);
            }

            public static string ToHtml(Color c)
            {
                if (c.IsEmpty)
                    return String.Empty;

                if (c.IsSystemColor)
                {
                    var kc = Color.ToKnownColor();
                    switch (kc)
                    {
                        case KnownColor.ActiveBorder:
                        case KnownColor.ActiveCaption:
                        case KnownColor.AppWorkspace:
                        case KnownColor.GrayText:
                        case KnownColor.Highlight:
                        case KnownColor.HighlightText:
                        case KnownColor.InactiveBorder:
                        case KnownColor.InactiveCaption:
                        case KnownColor.InactiveCaptionText:
                        case KnownColor.InfoText:
                        case KnownColor.Menu:
                        case KnownColor.MenuText:
                        case KnownColor.ScrollBar:
                        case KnownColor.Window:
                        case KnownColor.WindowFrame:
                        case KnownColor.WindowText:
                            return KnownColors.GetName(kc).ToLowerInvariant();

                        case KnownColor.ActiveCaptionText:
                            return "captiontext";
                        case KnownColor.Control:
                            return "buttonface";
                        case KnownColor.ControlDark:
                            return "buttonshadow";
                        case KnownColor.ControlDarkDark:
                            return "threeddarkshadow";
                        case KnownColor.ControlLight:
                            return "buttonface";
                        case KnownColor.ControlLightLight:
                            return "buttonhighlight";
                        case KnownColor.ControlText:
                            return "buttontext";
                        case KnownColor.Desktop:
                            return "background";
                        case KnownColor.HotTrack:
                            return "highlight";
                        case KnownColor.Info:
                            return "infobackground";

                        default:
                            return String.Empty;
                    }
                }

                if (c.IsNamedColor)
                {
                    return c == Color.LightGray ? "LightGrey" : c.Name;
                }

                return FormatHtml(c.R, c.G, c.B);
            }

            static char GetHexNumber(int b)
            {
                return (char)(b > 9 ? 55 + b : 48 + b);
            }

            static string FormatHtml(int r, int g, int b)
            {
                var htmlColor = new char[7];
                htmlColor[0] = '#';
                htmlColor[1] = GetHexNumber((r >> 4) & 15);
                htmlColor[2] = GetHexNumber(r & 15);
                htmlColor[3] = GetHexNumber((g >> 4) & 15);
                htmlColor[4] = GetHexNumber(g & 15);
                htmlColor[5] = GetHexNumber((b >> 4) & 15);
                htmlColor[6] = GetHexNumber(b & 15);

                return new string(htmlColor);
            }

            public static int ToOle(Color c)
            {
                // OleColor format is BGR, same as Win32
                return ((c.B << 16) | (c.G << 8) | c.R);
            }

            public static int ToWin32(Color c)
            {
                // Win32Color format is BGR, Same as OleColor
                return ((c.B << 16) | (c.G << 8) | c.R);
            }
        }

        internal static class KnownColors
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

            public static string GetName(short kc)
            {
                switch (kc)
                {
                    case 1: return "ActiveBorder";
                    case 2: return "ActiveCaption";
                    case 3: return "ActiveCaptionText";
                    case 4: return "AppWorkspace";
                    case 5: return "Control";
                    case 6: return "ControlDark";
                    case 7: return "ControlDarkDark";
                    case 8: return "ControlLight";
                    case 9: return "ControlLightLight";
                    case 10: return "ControlText";
                    case 11: return "Desktop";
                    case 12: return "GrayText";
                    case 13: return "Highlight";
                    case 14: return "HighlightText";
                    case 15: return "HotTrack";
                    case 16: return "InactiveBorder";
                    case 17: return "InactiveCaption";
                    case 18: return "InactiveCaptionText";
                    case 19: return "Info";
                    case 20: return "InfoText";
                    case 21: return "Menu";
                    case 22: return "MenuText";
                    case 23: return "ScrollBar";
                    case 24: return "Window";
                    case 25: return "WindowFrame";
                    case 26: return "WindowText";
                    case 27: return "Transparent";
                    case 28: return "AliceBlue";
                    case 29: return "AntiqueWhite";
                    case 30: return "Aqua";
                    case 31: return "Aquamarine";
                    case 32: return "Azure";
                    case 33: return "Beige";
                    case 34: return "Bisque";
                    case 35: return "Black";
                    case 36: return "BlanchedAlmond";
                    case 37: return "Blue";
                    case 38: return "BlueViolet";
                    case 39: return "Brown";
                    case 40: return "BurlyWood";
                    case 41: return "CadetBlue";
                    case 42: return "Chartreuse";
                    case 43: return "Chocolate";
                    case 44: return "Coral";
                    case 45: return "CornflowerBlue";
                    case 46: return "Cornsilk";
                    case 47: return "Crimson";
                    case 48: return "Cyan";
                    case 49: return "DarkBlue";
                    case 50: return "DarkCyan";
                    case 51: return "DarkGoldenrod";
                    case 52: return "DarkGray";
                    case 53: return "DarkGreen";
                    case 54: return "DarkKhaki";
                    case 55: return "DarkMagenta";
                    case 56: return "DarkOliveGreen";
                    case 57: return "DarkOrange";
                    case 58: return "DarkOrchid";
                    case 59: return "DarkRed";
                    case 60: return "DarkSalmon";
                    case 61: return "DarkSeaGreen";
                    case 62: return "DarkSlateBlue";
                    case 63: return "DarkSlateGray";
                    case 64: return "DarkTurquoise";
                    case 65: return "DarkViolet";
                    case 66: return "DeepPink";
                    case 67: return "DeepSkyBlue";
                    case 68: return "DimGray";
                    case 69: return "DodgerBlue";
                    case 70: return "Firebrick";
                    case 71: return "FloralWhite";
                    case 72: return "ForestGreen";
                    case 73: return "Fuchsia";
                    case 74: return "Gainsboro";
                    case 75: return "GhostWhite";
                    case 76: return "Gold";
                    case 77: return "Goldenrod";
                    case 78: return "Gray";
                    case 79: return "Green";
                    case 80: return "GreenYellow";
                    case 81: return "Honeydew";
                    case 82: return "HotPink";
                    case 83: return "IndianRed";
                    case 84: return "Indigo";
                    case 85: return "Ivory";
                    case 86: return "Khaki";
                    case 87: return "Lavender";
                    case 88: return "LavenderBlush";
                    case 89: return "LawnGreen";
                    case 90: return "LemonChiffon";
                    case 91: return "LightBlue";
                    case 92: return "LightCoral";
                    case 93: return "LightCyan";
                    case 94: return "LightGoldenrodYellow";
                    case 95: return "LightGray";
                    case 96: return "LightGreen";
                    case 97: return "LightPink";
                    case 98: return "LightSalmon";
                    case 99: return "LightSeaGreen";
                    case 100: return "LightSkyBlue";
                    case 101: return "LightSlateGray";
                    case 102: return "LightSteelBlue";
                    case 103: return "LightYellow";
                    case 104: return "Lime";
                    case 105: return "LimeGreen";
                    case 106: return "Linen";
                    case 107: return "Magenta";
                    case 108: return "Maroon";
                    case 109: return "MediumAquamarine";
                    case 110: return "MediumBlue";
                    case 111: return "MediumOrchid";
                    case 112: return "MediumPurple";
                    case 113: return "MediumSeaGreen";
                    case 114: return "MediumSlateBlue";
                    case 115: return "MediumSpringGreen";
                    case 116: return "MediumTurquoise";
                    case 117: return "MediumVioletRed";
                    case 118: return "MidnightBlue";
                    case 119: return "MintCream";
                    case 120: return "MistyRose";
                    case 121: return "Moccasin";
                    case 122: return "NavajoWhite";
                    case 123: return "Navy";
                    case 124: return "OldLace";
                    case 125: return "Olive";
                    case 126: return "OliveDrab";
                    case 127: return "Orange";
                    case 128: return "OrangeRed";
                    case 129: return "Orchid";
                    case 130: return "PaleGoldenrod";
                    case 131: return "PaleGreen";
                    case 132: return "PaleTurquoise";
                    case 133: return "PaleVioletRed";
                    case 134: return "PapayaWhip";
                    case 135: return "PeachPuff";
                    case 136: return "Peru";
                    case 137: return "Pink";
                    case 138: return "Plum";
                    case 139: return "PowderBlue";
                    case 140: return "Purple";
                    case 141: return "Red";
                    case 142: return "RosyBrown";
                    case 143: return "RoyalBlue";
                    case 144: return "SaddleBrown";
                    case 145: return "Salmon";
                    case 146: return "SandyBrown";
                    case 147: return "SeaGreen";
                    case 148: return "SeaShell";
                    case 149: return "Sienna";
                    case 150: return "Silver";
                    case 151: return "SkyBlue";
                    case 152: return "SlateBlue";
                    case 153: return "SlateGray";
                    case 154: return "Snow";
                    case 155: return "SpringGreen";
                    case 156: return "SteelBlue";
                    case 157: return "Tan";
                    case 158: return "Teal";
                    case 159: return "Thistle";
                    case 160: return "Tomato";
                    case 161: return "Turquoise";
                    case 162: return "Violet";
                    case 163: return "Wheat";
                    case 164: return "White";
                    case 165: return "WhiteSmoke";
                    case 166: return "Yellow";
                    case 167: return "YellowGreen";
                    default: return String.Empty;
                }
            }

            public static string GetName(KnownColor kc)
            {
                return GetName((short)kc);
            }

            public static Color FindColorMatch(Color c)
            {
                var argb = (uint)c.ToArgb();
                for (int i = 0; i < KnownColors.ArgbValues.Length; i++)
                {
                    if (argb == KnownColors.ArgbValues[i])
                        return KnownColors.FromKnownColor((KnownColor)i);
                }
                return Color.Empty;
            }

            public static bool TryFindKnownColorMatch(Color c, out KnownColor knownColor)
            {
                var argb = (uint)c.ToArgb();
                for (int i = 0; i < KnownColors.ArgbValues.Length; i++)
                {
                    if (argb == KnownColors.ArgbValues[i])
                    {
                        knownColor = (KnownColor)i;
                        return true;
                    }
                }
                knownColor = KnownColor.ActiveBorder;
                return false;
            }

            // When this method is called, we teach any new color(s) to the Color class
            // NOTE: This is called (reflection) by System.Windows.Forms.Theme (this isn't dead code)
            public static void Update(int knownColor, int color)
            {
                ArgbValues[knownColor] = (uint)color;
            }
        }

        internal sealed class SystemColors
        {

            // not creatable...
            //
            private SystemColors()
            {
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.ActiveBorder"]/*' />
            /// <devdoc>
            ///     The color of the filled area of an active window border.
            /// </devdoc>
            public static Color ActiveBorder
            {
                get
                {
                    return new Color(KnownColor.ActiveBorder);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.ActiveCaption"]/*' />
            /// <devdoc>
            ///     The color of the background of an active title bar caption.
            /// </devdoc>
            public static Color ActiveCaption
            {
                get
                {
                    return new Color(KnownColor.ActiveCaption);
                }
            }

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

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.AppWorkspace"]/*' />
            /// <devdoc>
            ///     The color of the application workspace.  The application workspace
            ///     is the area in a multiple document view that is not being occupied
            ///     by documents.
            /// </devdoc>
            public static Color AppWorkspace
            {
                get
                {
                    return new Color(KnownColor.AppWorkspace);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.ButtonFace"]/*' />
            /// <devdoc>
            ///     Face color for three-dimensional display elements and for dialog box backgrounds.
            /// </devdoc>
            public static Color ButtonFace
            {
                get
                {
                    return new Color(KnownColor.ButtonFace);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.ButtonHighlight"]/*' />
            /// <devdoc>
            ///     Highlight color for three-dimensional display elements (for edges facing the light source.)
            /// </devdoc>
            public static Color ButtonHighlight
            {
                get
                {
                    return new Color(KnownColor.ButtonHighlight);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.ButtonShadow"]/*' />
            /// <devdoc>
            ///     Shadow color for three-dimensional display elements (for edges facing away from the light source.)
            /// </devdoc>
            public static Color ButtonShadow
            {
                get
                {
                    return new Color(KnownColor.ButtonShadow);
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

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.GradientActiveCaption"]/*' />
            /// <devdoc>
            ///     Right side color in the color gradient of an active window's title bar. 
            ///     The ActiveCaption Color specifies the left side color.
            /// </devdoc>
            public static Color GradientActiveCaption
            {
                get
                {
                    return new Color(KnownColor.GradientActiveCaption);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.GradientInactiveCaption"]/*' />
            /// <devdoc>
            ///     Right side color in the color gradient of an inactive window's title bar. 
            ///     The InactiveCaption Color specifies the left side color.
            /// </devdoc>
            public static Color GradientInactiveCaption
            {
                get
                {
                    return new Color(KnownColor.GradientInactiveCaption);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.GrayText"]/*' />
            /// <devdoc>
            ///     The color of text that is being shown in a disabled, or grayed-out
            ///     state.
            /// </devdoc>
            public static Color GrayText
            {
                get
                {
                    return new Color(KnownColor.GrayText);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.Highlight"]/*' />
            /// <devdoc>
            ///     The color of the background of highlighted text.  This includes
            ///     selected menu items as well as selected text.
            /// </devdoc>
            public static Color Highlight
            {
                get
                {
                    return new Color(KnownColor.Highlight);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.HighlightText"]/*' />
            /// <devdoc>
            ///     The color of the text of highlighted text.  This includes
            ///     selected menu items as well as selected text.
            /// </devdoc>
            public static Color HighlightText
            {
                get
                {
                    return new Color(KnownColor.HighlightText);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.HotTrack"]/*' />
            /// <devdoc>
            ///     The hot track color.
            /// </devdoc>
            public static Color HotTrack
            {
                get
                {
                    return new Color(KnownColor.HotTrack);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.InactiveBorder"]/*' />
            /// <devdoc>
            ///     The color of the filled area of an inactive window border.
            /// </devdoc>
            public static Color InactiveBorder
            {
                get
                {
                    return new Color(KnownColor.InactiveBorder);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.InactiveCaption"]/*' />
            /// <devdoc>
            ///     The color of the background of an inactive title bar caption.
            /// </devdoc>
            public static Color InactiveCaption
            {
                get
                {
                    return new Color(KnownColor.InactiveCaption);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.InactiveCaptionText"]/*' />
            /// <devdoc>
            ///     The color of the text of an inactive title bar caption.
            /// </devdoc>
            public static Color InactiveCaptionText
            {
                get
                {
                    return new Color(KnownColor.InactiveCaptionText);
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

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.InfoText"]/*' />
            /// <devdoc>
            ///     The color of the info/tool tip text.
            /// </devdoc>
            public static Color InfoText
            {
                get
                {
                    return new Color(KnownColor.InfoText);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.Menu"]/*' />
            /// <devdoc>
            ///     The color of the background of a menu.
            /// </devdoc>
            public static Color Menu
            {
                get
                {
                    return new Color(KnownColor.Menu);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.MenuBar"]/*' />
            /// <devdoc>
            ///     The color of the background of a menu bar.
            /// </devdoc>
            public static Color MenuBar
            {
                get
                {
                    return new Color(KnownColor.MenuBar);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.MenuHighlight"]/*' />
            /// <devdoc>
            ///     The color used to highlight menu items when the menu appears as a flat menu. 
            ///     The highlighted menu item is outlined with the Highlight Color.
            /// </devdoc>
            public static Color MenuHighlight
            {
                get
                {
                    return new Color(KnownColor.MenuHighlight);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.MenuText"]/*' />
            /// <devdoc>
            ///     The color of the text on a menu.
            /// </devdoc>
            public static Color MenuText
            {
                get
                {
                    return new Color(KnownColor.MenuText);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.ScrollBar"]/*' />
            /// <devdoc>
            ///     The color of the scroll bar area that is not being used by the
            ///     thumb button.
            /// </devdoc>
            public static Color ScrollBar
            {
                get
                {
                    return new Color(KnownColor.ScrollBar);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.Window"]/*' />
            /// <devdoc>
            ///     The color of the client area of a window.
            /// </devdoc>
            public static Color Window
            {
                get
                {
                    return new Color(KnownColor.Window);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.WindowFrame"]/*' />
            /// <devdoc>
            ///     The color of the thin frame drawn around a window.
            /// </devdoc>
            public static Color WindowFrame
            {
                get
                {
                    return new Color(KnownColor.WindowFrame);
                }
            }

            /// <include file='doc\SystemColors.uex' path='docs/doc[@for="SystemColors.WindowText"]/*' />
            /// <devdoc>
            ///     The color of the text in the client area of a window.
            /// </devdoc>
            public static Color WindowText
            {
                get
                {
                    return new Color(KnownColor.WindowText);
                }
            }
        }
    }
}