using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace OpenVIII.Encoding
{
    // ReSharper disable once InconsistentNaming
    public sealed class FF8TextEncodingCodepage
    {
        #region Fields

        private static HashSet<ushort> unsupporedCharacters = new HashSet<ushort>();

        /// <summary>
        /// Characters not in the codepage but have alternative characters that can be used.
        /// </summary>
        private readonly Dictionary<char, char> _alts;

        private readonly Dictionary<char, byte> _bytes;
        private readonly char[] _chars;

        #endregion Fields

        #region Constructors

        public FF8TextEncodingCodepage(char[] chars, Dictionary<char, byte> bytes)
        {
            _chars = chars ?? throw new ArgumentNullException(nameof(chars));
            _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
            _alts = GetAlts();
        }

        #endregion Constructors

        #region Indexers

        public char this[byte b]
        {
            get
            {
                var c = _chars[b];
                return c == '\0' ? '�' : c;
            }
        }

        public byte this[char c]
        {
            get
            {
                if (_bytes.ContainsKey(c))
                    return _bytes[c];
                else if (_alts.ContainsKey(c))
                    return _bytes[_alts[c]];
                else
                {
                    //Debug.WriteLine(
                    //    $@"Character 0x{(ushort) c:x4} '{c}' is unsupported. please add to alts or codepage");
                    unsupporedCharacters.Add(c);
                    Debug.WriteLine(
                        $@"All Unsupported Characters, please add to alts or codepage '{ string.Join(", ", unsupporedCharacters.Select(x => $"0x{x:x4} '{(char)x}'"))}'");
                }

                return _bytes[' '];
            }
        }

        #endregion Indexers

        #region Methods

        public static FF8TextEncodingCodepage Create()
        {
            var chars = CreateDefaultEncoding();

            var bytes = new Dictionary<char, byte>(chars.Length);
            for (var i = chars.Length - 1; i >= 0; i--)
            {
                var ch = chars[i];
                switch (ch)
                {
                    case '¥':
                    case '☻':
                    case 'ⱷ':
                        chars[i] = '\0';
                        continue;
                    case '\0':
                        continue;
                }

                bytes[chars[i]] = (byte)i;
            }
            return new FF8TextEncodingCodepage(chars, bytes);
        }

        public void GetParameters(out char[] chars, out HashSet<char>[] bytes)
        {
            chars = (char[])_chars.Clone();

            bytes = new HashSet<char>[256];
            for (var i = 0; i < 256; i++)
                bytes[i] = new HashSet<char>();

            foreach (var pair in _bytes)
                bytes[pair.Value].Add(pair.Key);
        }

        public byte? TryGetByte(char c)
        {
            if (_bytes.TryGetValue(c, out var b))
                return b;
            else if (_alts.ContainsKey(c) && _bytes.TryGetValue(_alts[c], out b))
                return b;
            return null;
        }

        public char? TryGetChar(byte b)
        {
            var c = _chars[b];
            if (c == '\0')
                return null;
            return c;
        }

        private static char[] CreateDefaultEncoding()
        {
            switch (CultureInfo.CurrentCulture.TwoLetterISOLanguageName)
            {
                case "jp":
                    return CreateJapaneseCodepage();

                case "ru":
                    return CreateRussianCodepage();

                default:
                    return CreateEuropeanCodepage();
            }
        }

        private static char[] CreateEuropeanCodepage() => new[] {
                '\0', '\0', '\n', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0',
                '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '®', '®', '®', '®', '\0', '\0', '\0',
                ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '%', '/', ':', '!', '?',
                '…', '+', '-', '=', '*', '&', '「', '」', '(', ')', '·', '.', ',', '~', '“', '”',
                '‘', '#', '$', '"', '_', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K',
                'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a',
                'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q',
                'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'À', 'Á', 'Â', 'Ä', 'Ç', 'È', 'É',
                'Ê', 'Ë', 'Ì', 'Í', 'Î', 'Ï', 'Ñ', 'Ò', 'Ó', 'Ô', 'Ö', 'Ù', 'Ú', 'Û', 'Ü', 'Œ',
                'ß', 'à', 'á', 'â', 'ä', 'ç', 'è', 'é', 'ê', 'ë', 'ì', 'í', 'î', 'ï', 'ñ', 'ò',
                'ó', 'ô', 'ö', 'ù', 'ú', 'û', 'ü', 'œ',
                'Ⅷ', '[', ']', '■', '◎', '♦', '〖', '〗',
                '□', '★', '『', '』', '▽', ';', '▼', '‾', '⨯', '☆', '¥', '↓', '°', '¡', '¿', '─',
                '«', '»', '±', '♬', '¥', '↑', '¥', '¥', '¥', '™', '<', '>', '¥', '¥', '¥', '¥',
                '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '\0', '\0',
                '¥', '¥', '¥', '☻', '☻', '☻', '☻', '☻', '☻', '☻', '☻', '\0', '\0', '☻', '☻', '☻',
                '☻', '☻', '☻', '☻', '☻', '☻', '☻', '☻', '☻', '\0', '☻', '\0', '☻', 'ⱷ', 'ⱷ', 'ⱷ'
            };

        private static char[] CreateJapaneseCodepage() => new[]
            {
                '\0', '\0', '\n', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0',
                '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '®', '®', '®', '®', '\0', '\0', '\0',
                'バ','ば','ビ','び','ブ','ぶ','ベ','べ','ボ','ぼ','ガ','が','ギ','ぎ','グ','ぐ',
                'ゲ','げ','ゴ','ご','ザ','ざ','ジ','じ','ズ','ず','ゼ','ぜ','ゾ','ぞ','ダ','だ',
                'ヂ','ぢ','ヅ','づ','デ','で','ド','ど','ヴ','パ','ぱ','ピ','ぴ','プ','ぷ','ペ',
                'ぺ','ポ','ぽ','０','１','２','３','４','５','６','７','８','９','、','。','　',
                'ハ','は','ヒ','ひ','フ','ふ','ヘ','へ','ホ','ほ','カ','か','キ','き','ク','く',
                'ケ','け','コ','こ','サ','さ','シ','し','ス','す','セ','せ','ソ','そ','タ','た',
                'チ','ち','ツ','つ','テ','て','ト','と','ウ','う','ア','あ','イ','い','エ','え',
                'オ','お','ナ','な','ニ','に','ヌ','ぬ','ネ','ね','ノ','の','マ','ま','ミ','み',
                'ム','む','メ','め','モ','も','ラ','ら','リ','り','ル','る','レ','れ','ロ','ろ',
                'ヤ','や','ユ','ゆ','ヨ','よ','ワ','わ','ン','ん','ヲ','を','ッ','っ','ャ','ゃ',
                'ュ','ゅ','ョ','ょ','ァ','ぁ','ィ','ぃ','ゥ','ぅ','ェ','ぇ','ォ','ぉ','Ａ','Ｂ','Ｃ',
                'Ｄ','Ｅ','Ｆ','Ｇ','Ｈ','Ｉ','Ｊ','Ｋ','Ｌ','Ｍ','Ｎ','Ｏ','Ｐ','Ｑ','Ｒ',
                'Ｓ','Ｔ','Ｕ','Ｖ','Ｗ','Ｘ','Ｙ','Ｚ','！','？','…','＋','－','＝','＊','／',
                '％','＆','「','」','（','）','収','容','所','駅','・','．','，','：','～','ー'
            };

        private static char[] CreateRussianCodepage() => new[]
            {
                '\0', '\0', '\n', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0',
                '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '®', '®', '®', '®', '\0', '\0', '\0',
                ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '%', '/', ':', '!', '?',
                '…', '+', '-', '=', '*', '&', '「', '」', '(', ')', '∙', '.', ',', '~', '”', '“',
                '‘', '#', '$', '’', '_', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K',
                'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a',
                'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q',
                'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'Б', 'Г', 'Д', 'Ё', 'Ж', 'З', 'И',
                'Й', 'Л', 'П', 'У', 'Ф', 'Ц', 'Ч', 'Ш', 'Щ', 'Ъ', 'Ы', 'Ь', 'Э', 'Ю', 'Я', 'б',
                'в', 'г', 'д', 'ж', 'з', 'й', 'к', 'л', 'м', 'н', 'п', 'т', 'ф', 'ц', 'ч', 'ш',
                'щ', 'ъ', 'ы', 'ь', 'э', 'ю', 'я', 'ё',
                'Ⅷ', '[', ']', '■', '◎', '♦', '〖', '〗',
                '□', '★', '『', '』', '▽', ';', '▼', '‾', '⨯', '☆', '¥', '↓', '°', '¡', '¿', '─',
                '«', '»', '±', '♬', '¥', '↑', '¥', '¥', '¥', '™', '<', '>', '¥', '¥', '¥', '¥',
                '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '¥', '\0', '\0',
                '¥', '¥', '¥', '☻', '☻', '☻', '☻', '☻', '☻', '☻', '☻', '\0', '\0', '☻', '☻', '☻',
                '☻', '☻', '☻', '☻', '☻', '☻', '☻', '☻', '☻', '\0', '☻', '\0', '☻', 'ⱷ', 'ⱷ', 'ⱷ'
            };

        private static Dictionary<char, char> GetAlts()
        {
            switch (CultureInfo.CurrentCulture.TwoLetterISOLanguageName)
            {
                case "jp":
                    return new Dictionary<char, char>
                    {
                        { '\'','・' },
                        { '{','「' },
                        { '}','」' },
                        { '\\', '／' },
                        { '\t', ' ' }
                    };

                case "ru":
                    return new Dictionary<char, char>
                    {
                        { '\'','‘' },
                        { '{','「' },
                        { '}','」' },
                        { '\\', '/' },
                        { '\t', ' ' }
                    };

                default:
                    return new Dictionary<char, char>
                    {
                        { '\'','‘' },
                        { '{','「' },
                        { '}','」' },
                        { '\\', '/' },
                        { '\t', ' ' }
                    };
            }
        }

        #endregion Methods
    }
}
