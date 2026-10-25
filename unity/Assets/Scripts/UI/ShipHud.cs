using UnityEngine;

// Screen overlay: title and deck, context prompt, controls, accepted order and the order card.
// Authored for a 1080p canvas and scaled to the window; key badges follow the active device.
public sealed class ShipHud : MonoBehaviour
{
    const float Canvas = 1080;
    static readonly Color Ink = new Color(.97f, .93f, .87f);
    static readonly Color Muted = new Color(.80f, .74f, .76f);
    static readonly Color Accent = new Color(.98f, .72f, .45f);
    static readonly Color Mint = new Color(.55f, .93f, .82f);
    static readonly Color Panel = new Color(.09f, .07f, .13f, .82f);
    static readonly Color KeyFill = new Color(.97f, .93f, .87f, .95f);

    CourierShip ship;
    GUIStyle eyebrow, heading, title, body, small, key, panel, card, button, buttonText;
    Texture2D panelTexture, cardTexture, keyTexture, buttonTexture, buttonHover, ruleTexture;

    public void Bind(CourierShip owner) => ship = owner;

    void OnGUI()
    {
        if (ship == null || !ship.Ready || ship.Controls == null) return;
        EnsureStyles();
        float scale = Mathf.Max(.55f, Screen.height / Canvas);
        var previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float width = Screen.width / scale, height = Screen.height / scale;
        DrawHeader();
        if (ship.Terminal.Accepted) DrawActiveOrder(width);
        DrawControls(height);
        if (ship.Terminal.IsOpen) DrawOrderCard(width, height);
        else DrawPrompt(width, height);
        GUI.matrix = previous;
    }

    string Key(CourierAction action) => ship.Controls.Label(action);

    void DrawHeader()
    {
        GUI.Box(new Rect(28, 24, 430, 112), GUIContent.none, panel);
        GUI.Label(new Rect(50, 38, 400, 22), "ОРБИТА  ·  МЕЖПЛАНЕТНАЯ ДОСТАВКА", eyebrow);
        GUI.Label(new Rect(50, 58, 400, 40), CourierCharacters.DisplayName, heading);
        GUI.DrawTexture(new Rect(50, 100, 18, 3), ruleTexture);
        GUI.Label(new Rect(76, 92, 380, 22), DeckName(ship.Deck), small);
    }

    static string DeckName(int deck) => deck < 0 ? "Уровень 00 · подземный ангар"
        : deck == ShipLayout.LivingDeck ? "Палуба 01 · гостиная и мостик" : "Палуба 02 · каюта";

    void DrawActiveOrder(float width)
    {
        var order = ship.Terminal.Order;
        var area = new Rect(width - 28 - 380, 24, 380, 112);
        GUI.Box(area, GUIContent.none, panel);
        GUI.Label(new Rect(area.x + 22, area.y + 14, 340, 22), "ТЕКУЩИЙ ЗАКАЗ  ·  " + order.Route, eyebrow);
        GUI.Label(new Rect(area.x + 22, area.y + 36, 340, 30), order.Cargo, body);
        GUI.Label(new Rect(area.x + 22, area.y + 72, 340, 26), "→ " + order.NextStep, small);
    }

    void DrawControls(float height)
    {
        var input = ship.Controls;
        float x = 28, y = height - 56;
        x = Hint(x, y, input.MoveLabel, "идти");
        x = Hint(x, y, Key(CourierAction.Sprint), "быстрее");
        x = Hint(x, y, Key(CourierAction.Interact), "действие");
        x = Hint(x, y, Key(CourierAction.Camera), "камера");
        if (!input.UsingGamepad) x = Hint(x,y,"ПКМ / колесо","обзор / зум");
        Hint(x, y, Key(CourierAction.Cancel), "закрыть");
    }

    float Hint(float x, float y, string keyLabel, string text)
    {
        x = Badge(x, y, keyLabel) + 8;
        float textWidth = small.CalcSize(new GUIContent(text)).x;
        GUI.Label(new Rect(x, y + 4, textWidth, 26), text, small);
        return x + textWidth + 22;
    }

    float Badge(float x, float y, string label)
    {
        float badgeWidth = Mathf.Max(34, key.CalcSize(new GUIContent(label)).x + 18);
        GUI.Box(new Rect(x, y, badgeWidth, 32), label, key);
        return x + badgeWidth;
    }

    void DrawPrompt(float width, float height)
    {
        string interact = Key(CourierAction.Interact), descend = Key(CourierAction.Descend);
        var parts = ship.LiftPrompt switch
        {
            LiftPrompt.RidingToHangar => new[] { ("", "Лифт · спуск в ангар") },
            LiftPrompt.RidingToCabin => new[] { ("", "Лифт · подъём в каюту") },
            LiftPrompt.RidingToLounge => new[] { ("", "Лифт · в гостиную") },
            LiftPrompt.Busy => new[] { ("", "Лифт движется") },
            LiftPrompt.Call => new[] { (interact, "Вызвать лифт") },
            LiftPrompt.EnterFromLeft => new[] { ("", "Вход в лифт — через левую дверь") },
            LiftPrompt.LoungeChoices => new[] { (interact, "В каюту"), (descend, "В ангар") },
            LiftPrompt.CabinChoices => new[] { (interact, "В гостиную"), (descend, "В ангар") },
            LiftPrompt.ToLounge => new[] { (interact, "В гостиную") },
            _ => ship.AtTerminal ? new[] { (interact, "Открыть заказ") } : null,
        };
        if (parts == null) return;
        float total = 0;
        foreach (var (badge, text) in parts) total += PromptWidth(badge, text);
        float x = (width - total) * .5f, y = height - 150;
        GUI.Box(new Rect(x - 16, y - 10, total + 32, 54), GUIContent.none, panel);
        foreach (var (badge, text) in parts)
        {
            float start = x;
            if (badge.Length > 0) x = Badge(x, y + 1, badge) + 10;
            GUI.Label(new Rect(x, y + 3, 400, 30), text, body);
            x = start + PromptWidth(badge, text);
        }
    }

    float PromptWidth(string badge, string text)
    {
        float width = body.CalcSize(new GUIContent(text)).x + 26;
        if (badge.Length > 0) width += Mathf.Max(34, key.CalcSize(new GUIContent(badge)).x + 18) + 10;
        return width;
    }

    void DrawOrderCard(float width, float height)
    {
        var terminal = ship.Terminal;
        var order = terminal.Order;
        var area = new Rect((width - 560) * .5f, (height - 470) * .5f, 560, 470);
        GUI.Box(area, GUIContent.none, card);
        float x = area.x + 36, w = area.width - 72, y = area.y + 30;
        GUI.Label(new Rect(x, y, w, 22), "ТЕРМИНАЛ ЗАКАЗОВ  ·  " + order.Route, eyebrow);
        GUI.Label(new Rect(x, y + 26, w, 44), order.Title, title);
        GUI.DrawTexture(new Rect(x, y + 80, w, 1), ruleTexture);
        Row(x, y + 96, w, "Получатель", order.Client);
        Row(x, y + 132, w, "Груз", order.Cargo);
        Row(x, y + 168, w, "Награда", order.Reward + " кредитов");
        GUI.Label(new Rect(x, y + 212, w, 60), order.Note, body);
        if (terminal.Accepted)
            GUI.Label(new Rect(x, y + 282, w, 28), "Заказ принят · " + order.NextStep, Colored(body, Mint));
        float buttonsY = area.yMax - 76;
        if (!terminal.Accepted && ActionButton(new Rect(x, buttonsY, 300, 46), Key(CourierAction.Interact), "Принять заказ"))
            ship.AcceptOrClose();
        if (ActionButton(new Rect(area.xMax - 36 - 180, buttonsY, 180, 46), Key(CourierAction.Cancel), "Закрыть"))
            terminal.Close();
    }

    void Row(float x, float y, float width, string label, string value)
    {
        GUI.Label(new Rect(x, y, 150, 28), label, small);
        GUI.Label(new Rect(x + 150, y - 2, width - 150, 30), value, body);
    }

    bool ActionButton(Rect area, string badge, string text)
    {
        bool clicked = GUI.Button(area, GUIContent.none, button);
        float badgeEnd = Badge(area.x + 10, area.y + 7, badge);
        GUI.Label(new Rect(badgeEnd + 10, area.y + 9, area.width - (badgeEnd - area.x) - 16, 30), text, buttonText);
        return clicked;
    }

    static GUIStyle Colored(GUIStyle style, Color color)
    {
        var copy = new GUIStyle(style);
        copy.normal.textColor = color;
        return copy;
    }

    void EnsureStyles()
    {
        if (panel != null) return;
        panelTexture = Rounded(Panel, 14);
        cardTexture = Rounded(new Color(.11f, .085f, .16f, .96f), 18);
        keyTexture = Rounded(KeyFill, 7);
        buttonTexture = Rounded(new Color(.98f, .72f, .45f, .22f), 10);
        buttonHover = Rounded(new Color(.98f, .72f, .45f, .38f), 10);
        ruleTexture = Solid(new Color(Accent.r, Accent.g, Accent.b, .7f));
        panel = Frame(panelTexture, 14);
        card = Frame(cardTexture, 18);
        button = Frame(buttonTexture, 10);
        button.hover.background = buttonHover;
        button.active.background = buttonHover;
        eyebrow = Text(13, Accent, FontStyle.Bold);
        heading = Text(30, Ink, FontStyle.Bold);
        title = Text(27, Ink, FontStyle.Bold);
        body = Text(18, Ink);
        small = Text(15, Muted);
        buttonText = Text(18, Ink, FontStyle.Bold);
        key = new GUIStyle(GUI.skin.box) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(7, 7, 7, 7), padding = new RectOffset(8, 8, 2, 2) };
        key.normal.background = keyTexture;
        key.normal.textColor = new Color(.13f, .10f, .16f);
    }

    static GUIStyle Text(int size, Color color, FontStyle style = FontStyle.Normal)
    {
        var text = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = style, wordWrap = true, richText = false };
        text.normal.textColor = color;
        return text;
    }

    static GUIStyle Frame(Texture2D texture, int radius)
    {
        var style = new GUIStyle { border = new RectOffset(radius, radius, radius, radius) };
        style.normal.background = texture;
        return style;
    }

    // Anti-aliased rounded rectangle, nine-sliced by the style border.
    static Texture2D Rounded(Color fill, int radius)
    {
        int size = radius * 2 + 2;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Distance from the corner circle's centre; positive only inside a corner zone.
                float dx = Mathf.Abs(x + .5f - size * .5f) - (size * .5f - radius);
                float dy = Mathf.Abs(y + .5f - size * .5f) - (size * .5f - radius);
                float distance = new Vector2(Mathf.Max(dx, 0), Mathf.Max(dy, 0)).magnitude;
                var color = fill;
                color.a *= Mathf.Clamp01(radius - distance + .5f);
                pixels[y * size + x] = color;
            }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    static Texture2D Solid(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    void OnDestroy()
    {
        foreach (var texture in new[] { panelTexture, cardTexture, keyTexture, buttonTexture, buttonHover, ruleTexture })
            if (texture != null) Destroy(texture);
    }
}
