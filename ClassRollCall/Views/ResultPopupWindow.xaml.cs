using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ClassRollCall.Views;

public partial class ResultPopupWindow : Window
{
    private readonly DispatcherTimer _closeTimer;
    private readonly int _totalSeconds;
    private double _remainingSeconds;
    private bool _autoCloseEnabled = true;
    private bool _isClosing;

    private readonly List<string> _names;
    private readonly Random _rng = new();
    private const int MaxCards = 4;
    private const int MaxPerCard = 5;

    private readonly List<Border> _borders = new();
    private readonly List<Card> _cards = new();
    private readonly List<DispatcherTimer> _timers = new();

    private class Card
    {
        public Grid ContentGrid = null!;
        public Grid LoadingDots = null!;
        public StackPanel NamesPanel = null!;
        public List<TextBlock> NameBlocks = null!;
        public ProgressBar ProgressBar = null!;
        public double TargetW, TargetH;
        public bool NamesShown;
    }

    private static readonly Color Bg = Color.FromRgb(0x2B, 0x2B, 0x2B);
    private static readonly Color Fg = Colors.White;
    private static readonly Color Sub = Color.FromRgb(0x99, 0x99, 0x99);

    public ResultPopupWindow(List<string> names)
    {
        InitializeComponent();
        _names = names;
        _totalSeconds = 3 + Math.Max(0, _names.Count - 1);
        _remainingSeconds = _totalSeconds;
        _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _closeTimer.Tick += CloseTimer_Tick;
        Loaded += OnLoaded;
        MouseLeftButtonDown += OnAnyClick;
    }

    // ==================== 宽度计算 ====================

    /// <summary>估算文本像素宽度（中文≈字号，英文≈字号×0.6）</summary>
    private static double MeasureTextWidth(string text, int fontSize)
    {
        double w = 0;
        foreach (char c in text)
        {
            if (c > 127) w += fontSize;       // 中文/全角
            else w += fontSize * 0.6;          // 英文/数字/符号
        }
        return w;
    }

    /// <summary>根据名字列表计算合适卡片宽度</summary>
    private static double CalcCardWidth(List<string> names, int fontSize, int cardCount)
    {
        double maxTextWidth = names.Max(n => MeasureTextWidth(n, fontSize));
        // 卡片宽度 = 文字宽度 + 左右内边距(40) + 卡片间距
        double contentWidth = maxTextWidth + 80;
        double minW = 260;
        double maxW = cardCount == 1 ? 500 : 380;
        return Math.Clamp(contentWidth, minW, maxW);
    }

    // ==================== 入场 ====================

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        fade.Completed += (s, a) => BuildAll();
        RootGrid.BeginAnimation(OpacityProperty, fade);
    }

    private void BuildAll()
    {
        int cardCount = _names.Count <= MaxPerCard ? 1
            : Math.Min(MaxCards, (int)Math.Ceiling(_names.Count / 4.0));
        int per = (int)Math.Ceiling((double)_names.Count / cardCount);

        var groups = new List<List<string>>();
        for (int i = 0; i < cardCount; i++)
            groups.Add(_names.Skip(i * per).Take(per).ToList());

        foreach (var g in groups)
        {
            var (border, card) = MakeCard(g, _borders.Count + 1, groups.Count);
            CardPanel.Children.Add(border);
            _borders.Add(border);
            _cards.Add(card);
        }

        // 全部卡片一起展开
        int expanded = 0;
        for (int i = 0; i < _cards.Count; i++)
        {
            int idx = i;
            ExpandCardFrame(idx, () =>
            {
                expanded++;
                if (expanded >= _cards.Count)
                {
                    ShowNames(0);
                    if (_cards.Count > 0) _closeTimer.Start();
                    for (int j = 1; j < _cards.Count; j++)
                    {
                        int jdx = j;
                        double delay = jdx * 1200 + _rng.Next(800, 2000);
                        After(delay, () => ShowNames(jdx));
                    }
                }
            });
        }
    }

    private (Border, Card) MakeCard(List<string> names, int idx, int total)
    {
        var card = new Card();

        // ---- 三点加载 ----
        var dotsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        for (int i = 0; i < 3; i++)
        {
            var dot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
                Margin = new Thickness(4),
                RenderTransform = new ScaleTransform(0.4, 0.4),
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            var sa = new DoubleAnimation(0.4, 1.0, TimeSpan.FromMilliseconds(400))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromMilliseconds(i * 150),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            dot.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, sa);
            dot.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, sa);
            dotsPanel.Children.Add(dot);
        }
        card.LoadingDots = new Grid
        {
            Height = 50,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { dotsPanel }
        };

        // ---- 名字面板 ----
        var namesPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        card.NamesPanel = namesPanel;
        card.NameBlocks = new List<TextBlock>();

        int fs = names.Count <= 2 ? 44 : names.Count <= 4 ? 36 : 28;
        foreach (var name in names)
        {
            var tb = new TextBlock
            {
                Text = name,
                Foreground = new SolidColorBrush(Fg),
                FontSize = fs,
                FontWeight = FontWeights.Medium,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4),
                Opacity = 0,
                RenderTransform = new TranslateTransform(60, 0)
            };
            namesPanel.Children.Add(tb);
            card.NameBlocks.Add(tb);
        }

        // ---- 进度条 ----
        card.ProgressBar = new ProgressBar
        {
            Height = 6,
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF)),
            Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
            Maximum = 100,
            Value = 0,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0)
        };

        // ---- 内层布局 ----
        var inner = new Grid();
        inner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        inner.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        inner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        string title = total > 1 ? $"第 {idx} 组" : "点名结果";
        var titleTb = new TextBlock
        {
            Text = title,
            Foreground = new SolidColorBrush(Sub),
            FontSize = 14,
            FontWeight = FontWeights.Light,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 28, 0, 0)
        };
        Grid.SetRow(titleTb, 0);
        inner.Children.Add(titleTb);

        var midGrid = new Grid();
        midGrid.Children.Add(card.LoadingDots);
        midGrid.Children.Add(card.NamesPanel);
        Grid.SetRow(midGrid, 1);
        inner.Children.Add(midGrid);

        Grid.SetRow(card.ProgressBar, 2);
        inner.Children.Add(card.ProgressBar);

        card.ContentGrid = new Grid { Opacity = 0, Children = { inner } };

        // ---- 自适应宽度 ----
        card.TargetW = CalcCardWidth(names, fs, total);
        card.TargetH = 160 + names.Count * 52;

        var border = new Border
        {
            Background = new SolidColorBrush(Bg),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Width = 0,
            Height = 4,
            MinHeight = 360,
            Margin = new Thickness(14, 0, 14, 0),
            Child = card.ContentGrid,
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 0.5,
                BlurRadius = 36,
                ShadowDepth = 0
            }
        };

        return (border, card);
    }

    // ==================== 展开 ====================

    private void ExpandCardFrame(int cardIdx, Action onDone)
    {
        if (_isClosing) { onDone(); return; }
        var card = _cards[cardIdx];
        var border = _borders[cardIdx];
        double tw = card.TargetW, th = card.TargetH;

        var wAnim = new DoubleAnimation(0, tw, TimeSpan.FromMilliseconds(260))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        wAnim.Completed += (s, a) =>
        {
            var hAnim = new DoubleAnimation(4, th, TimeSpan.FromMilliseconds(300))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            hAnim.Completed += (s2, a2) =>
            {
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160));
                fadeIn.Completed += (s3, a3) => onDone();
                card.ContentGrid.BeginAnimation(OpacityProperty, fadeIn);
            };
            border.BeginAnimation(HeightProperty, hAnim);
        };
        border.BeginAnimation(WidthProperty, wAnim);
    }

    private void ShowNames(int cardIdx)
    {
        if (_isClosing) return;
        var card = _cards[cardIdx];
        if (card.NamesShown) return;
        card.NamesShown = true;
        card.LoadingDots.Visibility = Visibility.Collapsed;
        card.NamesPanel.Visibility = Visibility.Visible;
        SlideIn(cardIdx, 0);
    }

    private void SlideIn(int cardIdx, int nameIdx)
    {
        if (_isClosing) return;
        var card = _cards[cardIdx];
        if (nameIdx >= card.NameBlocks.Count) return;
        var tb = card.NameBlocks[nameIdx];
        var tr = tb.RenderTransform as TranslateTransform ?? new TranslateTransform(60, 0);
        tb.RenderTransform = tr;
        tb.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        tr.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(60, 0, TimeSpan.FromMilliseconds(140))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        After(100, () => SlideIn(cardIdx, nameIdx + 1));
    }

    // ==================== 倒计时 ====================

    private void CloseTimer_Tick(object? sender, EventArgs e)
    {
        _remainingSeconds -= 0.05;
        double pct = Math.Min((1 - _remainingSeconds / _totalSeconds) * 100, 100);
        foreach (var c in _cards) c.ProgressBar.Value = pct;
        if (_remainingSeconds <= 0) { _closeTimer.Stop(); CloseWithAnimation(); }
    }

    private void OnAnyClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!_autoCloseEnabled) return;
        _autoCloseEnabled = false;
        _closeTimer.Stop();
        foreach (var c in _cards) c.ProgressBar.Visibility = Visibility.Collapsed;
        CloseBtn.Visibility = Visibility.Visible;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => CloseWithAnimation();

    // ==================== 退场 ====================

    private void CloseWithAnimation()
    {
        if (_isClosing) return;
        _isClosing = true;
        _closeTimer.Stop();
        StopTimers();
        CloseBtn.Visibility = Visibility.Collapsed;

        int done = 0, total = _cards.Count;
        for (int i = 0; i < _cards.Count; i++)
        {
            int idx = i;
            var card = _cards[idx];
            var border = _borders[idx];
            if (!card.NamesShown) { done++; if (done >= total) FinishClose(); continue; }
            SlideOut(idx, 0, () =>
            {
                var fo = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
                fo.Completed += (s, a) =>
                {
                    var ha = new DoubleAnimation(card.TargetH, 4, TimeSpan.FromMilliseconds(200))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
                    ha.Completed += (s2, a2) =>
                    {
                        var wa = new DoubleAnimation(card.TargetW, 0, TimeSpan.FromMilliseconds(160))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
                        wa.Completed += (s3, a3) =>
                        {
                            done++;
                            if (done >= total) FinishClose();
                        };
                        border.BeginAnimation(WidthProperty, wa);
                    };
                    border.BeginAnimation(HeightProperty, ha);
                };
                card.ContentGrid.BeginAnimation(OpacityProperty, fo);
            });
        }
        if (total == 0) FinishClose();
    }

    private void SlideOut(int cardIdx, int nameIdx, Action onDone)
    {
        var card = _cards[cardIdx];
        if (nameIdx >= card.NameBlocks.Count) { onDone(); return; }
        var tb = card.NameBlocks[nameIdx];
        var tr = tb.RenderTransform as TranslateTransform ?? new TranslateTransform(0, 0);
        tb.RenderTransform = tr;
        tb.BeginAnimation(OpacityProperty,
            new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(80))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });
        tr.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(0, 60, TimeSpan.FromMilliseconds(100))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });
        After(60, () => SlideOut(cardIdx, nameIdx + 1, onDone));
    }

    private void FinishClose()
    {
        var mf = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120));
        mf.Completed += (s, a) => Close();
        RootGrid.BeginAnimation(OpacityProperty, mf);
    }

    // ==================== 工具 ====================

    private void After(double ms, Action action)
    {
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
        t.Tick += (st, sa) => { t.Stop(); _timers.Remove(t); action(); };
        _timers.Add(t);
        t.Start();
    }

    private void StopTimers()
    {
        foreach (var t in _timers.ToList()) t.Stop();
        _timers.Clear();
    }
}
