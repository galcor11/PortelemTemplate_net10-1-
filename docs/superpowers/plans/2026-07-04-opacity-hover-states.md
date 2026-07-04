# Opacity & Hover States (Mimic Reference App) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Match the reference app's structure and visual state model — initial (resting) opacity vs. hover/active opacity for worm body placeholder segments, nav tabs, and buttons — plus the reference's page structure: one editor screen at a single URL with "עריכת השאלות" / "הגדרות כלליות" as tabs, a styled "טבלת המשחקים ←" back button, and the "השינויים נשמרים אוטומטית" / "השינויים נשמרו בהצלחה" status caption.

**Architecture:** Tasks 1–5 are CSS-only changes plus two tiny markup/state tweaks. Tasks 6–8 restructure the two editor pages (`QuestionsEditor`, `GeneralSettings`) into child components hosted by a new routable page `GameEditor` (`/GameEditor/{gameID:int}`), so both editor views live at the same URL and switch via in-page tab state (initial tab selectable with a `?tab=` query string). Blazor WASM client (`Client/`), scoped CSS per component (`.razor.css`). No server or model changes. Existing palette is reused: orange `#fd8a03` / `#cc5c00` / `#e67e22`, greens `#C5ECA5` / `#E9FDD9`, teal `#46BA9B`, disabled gray `#B7B7B7` / `#96928E`.

**Tech Stack:** .NET Blazor WebAssembly, scoped CSS isolation (`.razor.css`, `::deep`).

**Reference behavior extracted from the screenshots:**

1. **Placeholder body segments** (dashed `strokeBody.svg`): rest at reduced opacity (light gray look). While the editor is typing valid content in the add-item box (or hovering it), the *next empty slot's* dashed stroke turns dark/full-opacity to signal "the new item lands here". Whitespace-only input does **not** trigger the highlight (screenshot 1 note: two spaces keep the confirm button blocked).
2. **Add-item confirm button** ("אישור"): gray with `cursor: not-allowed` while the content is empty/whitespace-only; orange (primary style) once valid; returns to gray after a successful add resets the form.
3. **Nav tabs**: inactive tabs rest at reduced opacity; hover raises to full opacity; the active tab gets a pill highlight at full opacity.
4. **Buttons** (`MyButton` primary/secondary): need a hover state (slightly darker/tinted) in addition to the existing pressed (`:active`) state; disabled stays gray with `not-allowed`.
5. **Editor tabs on one screen**: "עריכת השאלות" (pencil) and "הגדרות כלליות" (gear) appear as two white rounded tab-buttons at the top of the *same* screen/URL. The active tab is full-opacity with an orange border; the inactive tab rests at reduced opacity and raises on hover. Today these are two separate routed pages (`/QuestionsEditor/{id}`, `/GeneralSettings/{id}`) — they must be merged under one host page.
6. **Back button**: a white pill button "טבלת המשחקים ←" at the top corner of the editor screen, navigating back to the games list. Today `GeneralSettings` has an unstyled `<a>` ("בחזרה למשחקים שלי") and `QuestionsEditor` has nothing.
7. **Status caption**: "השינויים נשמרים אוטומטית" sits at the bottom center of the editor at rest (screenshots 1–2); right after a successful save it switches to "השינויים נשמרו בהצלחה" (screenshot 3), then reverts.

**Testing note:** These are visual CSS states with no test infrastructure in this repo. Each task verifies with `dotnet build` (must succeed with 0 errors) plus a concrete manual check in the running app. Run the app with `dotnet run --project Server` and open the URL from `Server/Properties/launchSettings.json`.

---

## File Structure

New files (Tasks 6–8):

- `Client/Pages/GameEditor.razor` — new routable host page (`/GameEditor/{gameID:int}`): back button, tab header, active-tab state, status caption. Owns *all* chrome shown around the editor content in the screenshots.
- `Client/Pages/GameEditor.razor.css` — styles for the back pill, tab buttons (resting/hover/active opacity), and status caption.

Existing files:

- `Client/Components/Silky.razor` — compute "next slot" highlight class for the first empty placeholder.
- `Client/Components/Silky.razor.css` — resting opacity + highlight state for `.placeholder-segment`.
- `Client/Pages/QuestionsEditor.razor` — disabled state for the strawberry submit button (Task 2); becomes a non-routed child component and reports successful saves via `OnChangesSaved` (Tasks 6, 8).
- `Client/Pages/QuestionsEditor.razor.css` — strawberry submit button styles (gray/orange/hover) + dummy-worm structural styles for the add-question row.
- `Client/Pages/GeneralSettings.razor` — becomes a non-routed child component; save no longer navigates, it raises `OnSettingsSaved` (Task 7).
- `Client/Pages/GamesList.razor` — the two `NavigateTo` targets change to the new `GameEditor` route (Task 7).
- `Client/Shared/NavMenu.razor.css` — tab resting/hover/active opacity and active pill.
- `Client/Components/MyButton.razor.css` — hover states for primary/secondary.

**Task ordering:** Tasks 1–5 are independent of each other. Task 7 depends on Task 6; Task 8 depends on Tasks 6–7. Tasks 2 and 6 both edit `QuestionsEditor.razor` in different regions (submit buttons vs. file header) — either order works, resolve trivially.

---

### Task 1: Placeholder segment — resting opacity vs. "next slot" highlight

**Files:**
- Modify: `Client/Components/Silky.razor:94-117` (empty-placeholder loop)
- Modify: `Client/Components/Silky.razor.css:73-85`

- [ ] **Step 1: Add the next-slot class computation in the placeholder loop**

In `Client/Components/Silky.razor`, replace the empty-placeholder loop (currently lines 94–117) with:

```razor
@* לולאת for שפתחנו בשביל להציג את הפלייסהוֹלְדרים הריקים *@
@for (int p = 0; p < (2 - CurrentQuestion.Items.Count); p++)
{
    // הפלייסהולדר הריק הראשון הוא המקום שאליו ייכנס הפריט הבא -
    // הוא מודגש כשהעורך מרחף על התות או כשהוקלד בו תוכן אמיתי (לא רווחים בלבד)
    string nextSlotClass = "";
    if (p == 0 && (IsHoveringAddItem == true ||
        (CurrentStrawberryItem != null && CurrentStrawberryItem.answerID == 0
         && string.IsNullOrWhiteSpace(CurrentStrawberryItem.content) == false)))
    {
        nextSlotClass = "next-slot-highlight";
    }

    <div class="segment-wrapper">
        <div class="label-container">
            @* מופיע מעל הפלייסהולדר הראשון רק אם אין בכלל פריטים אמיתיים *@
            @if (CurrentQuestion.Items.Count == 0 && p == 0)
            {
                <EditableEdgeLabel LabelText="@CurrentQuestion.startLabel"
                                   DefaultText="ראשון"
                                   OnLabelSaved="@((newText) => OnStartLabelSaved.InvokeAsync(newText))"/>
            }

            @* תמיד יופיע מעל הפלייסהולדר האחרון כי הוא חותם את התולעת *@
            @if (p == (2 - CurrentQuestion.Items.Count) - 1)
            {
                <EditableEdgeLabel LabelText="@CurrentQuestion.endLabel"
                                   DefaultText="אחרון"
                                   OnLabelSaved="@((newText) => OnEndLabelSaved.InvokeAsync(newText))"/>
            }
        </div>
        <img src="SVG/strokeBody.svg" class="placeholder-segment @nextSlotClass" alt=""/>
    </div>
}
```

Only two things changed from the current markup: the `nextSlotClass` computation block at the top of the loop body, and `@nextSlotClass` appended to the `<img>` class list. Everything else is identical.

- [ ] **Step 2: Add resting opacity and the highlight state in scoped CSS**

In `Client/Components/Silky.razor.css`, replace the existing `.placeholder-segment` rule (lines 73–78) with:

```css
/* עיצוב הפלייסהולדר המקווקו של גוף התולעת - שקוף חלקית במצב מנוחה */
.placeholder-segment {
    width: 100px;
    height: auto;
    margin-left: 5px; /* מרווח קל בין הגופיפים */
    opacity: 0.55; /* מצב מנוחה - מקווקו בהיר כמו באפליקציית הייחוס */
    transition: opacity 0.3s ease, filter 0.3s ease;
}

/* הדגשת המשבצת שאליה ייכנס הפריט הבא - קו מקווקו כהה בשקיפות מלאה */
.next-slot-highlight {
    opacity: 1;
    filter: brightness(0.25); /* מכהה את ה-SVG לגוון כמעט שחור */
}
```

Keep `.floating-preview` (opacity 0.4) exactly as it is — it already matches the reference's faded "extra slot" preview.

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 4: Visual verification**

Run the app, open a game's QuestionsEditor, expand a question with 0 items:
- Both dashed segments render faded (~55% opacity).
- Hover the strawberry box → the first dashed segment darkens to near-black at full opacity.
- Type only spaces in the strawberry textarea, move the mouse away → segment stays faded.
- Type a real character, move the mouse away → segment stays dark (content-driven highlight).

- [ ] **Step 5: Commit**

```bash
git add Client/Components/Silky.razor Client/Components/Silky.razor.css
git commit -m "feat(client): fade placeholder segments, darken next slot on hover/typing"
```

---

### Task 2: Strawberry confirm button — gray/blocked when invalid, orange when valid

**Files:**
- Modify: `Client/Pages/QuestionsEditor.razor:189-197` (submit buttons) and the `@code` block
- Modify: `Client/Pages/QuestionsEditor.razor.css` (append new rules)

- [ ] **Step 1: Add a validity helper in the `@code` block**

In `Client/Pages/QuestionsEditor.razor`, inside the `@code` block (a good spot is right below the `currentStrawberryItem` field near line 579), add:

```csharp
    // הכפתור של התות מושבת כשאין תוכן אמיתי - רווחים בלבד לא נחשבים (כמו באפליקציית הייחוס)
    bool isStrawberryDisabled => string.IsNullOrWhiteSpace(currentStrawberryItem.content);
```

- [ ] **Step 2: Wire the class and disabled attribute onto both submit buttons**

Replace lines 189–197 (the submit-button conditional inside the `EditForm`) with:

```razor
                            <!-- כפתור ה-SUBMIT של התות שמשנה את הטקסט שלו -->
                            <!-- הכפתור אפור וחסום כשאין תוכן תקין, וכתום כשאפשר לאשר -->
                            @if (currentStrawberryItem.answerID > 0)
                            {
                                <button type="submit" class="strawberry-submit"
                                        disabled="@isStrawberryDisabled">שמירת שינויים</button>
                            }
                            else
                            {
                                <button type="submit" class="strawberry-submit"
                                        disabled="@isStrawberryDisabled">אישור</button>
                            }
```

- [ ] **Step 3: Style the button states in scoped CSS**

Append to `Client/Pages/QuestionsEditor.razor.css`:

```css
/* כפתור האישור של התות - עיצוב אחיד לשני המצבים עם מעבר חלק */
.strawberry-submit {
    border: none;
    border-radius: 6px;
    padding: 8px 22px;
    font-weight: bold;
    font-size: 15px;
    transition: background-color 0.2s ease, box-shadow 0.2s ease;
    /* מצב פעיל - כתום כמו הכפתור הראשי */
    background-color: #fd8a03;
    color: white;
    box-shadow: 0px 4px 0px #cc5c00;
    cursor: pointer;
}

/* ריחוף על הכפתור הפעיל - כתום כהה יותר */
.strawberry-submit:hover:not(:disabled) {
    background-color: #e67e22;
}

/* לחיצה - הכפתור "שוקע" כמו הכפתור הראשי */
.strawberry-submit:active:not(:disabled) {
    transform: translateY(4px);
    box-shadow: 0px 0px 0px #cc5c00;
}

/* מצב מושבת - אפור עם סמן עכבר חסום (עיגול חסום) כמו באפליקציית הייחוס */
.strawberry-submit:disabled {
    background-color: #B7B7B7;
    color: white;
    box-shadow: 0px 4px 0px #96928E;
    cursor: not-allowed;
}
```

Note: `cursor: not-allowed` does render on a `disabled` button; no `pointer-events` override is needed.

- [ ] **Step 4: Build**

Run: `dotnet build`
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 5: Visual verification**

In the running app, open a question's add-item box:
- Empty textarea → button gray, cursor shows the blocked circle, click does nothing.
- Type two spaces → still gray/blocked (matches screenshot 1's note).
- Type real text → button turns orange; hover darkens it; click adds the item.
- After a successful add the form resets → button returns to gray.
- Toggle to image mode: button stays gray until an image is uploaded (upload sets `content`), then turns orange.

- [ ] **Step 6: Commit**

```bash
git add Client/Pages/QuestionsEditor.razor Client/Pages/QuestionsEditor.razor.css
git commit -m "feat(client): gray blocked confirm button until item content is valid"
```

---

### Task 3: Nav tabs — resting opacity, hover opacity, active pill

**Files:**
- Modify: `Client/Shared/NavMenu.razor.css` (replace whole file — it is 10 lines)

- [ ] **Step 1: Replace the stylesheet**

Replace the full contents of `Client/Shared/NavMenu.razor.css` with:

```css
.navbar-dark{
    background-color: #46BA9B;
}

/* טאבים במצב מנוחה - שקיפות מופחתת כמו באפליקציית הייחוס */
::deep a.nav-link, button{
     color: white;
     opacity: 0.8;
     transition: opacity 0.2s ease, background-color 0.2s ease;
}

/* ריחוף - חוזרים לשקיפות מלאה עם קו תחתון */
::deep a.nav-link:hover, button:hover{
    text-decoration: underline;
    color: white;
    opacity: 1;
}

/* הטאב הפעיל - שקיפות מלאה, מודגש, ורקע "גלולה" מעוגל */
::deep a.nav-link.active {
    opacity: 1;
    font-weight: bold;
    background-color: rgba(255, 255, 255, 0.25);
    border-radius: 20px;
}
```

Blazor's `NavLink` adds the `active` class automatically when the route matches — no markup change is needed in `NavMenu.razor`.

- [ ] **Step 2: Build**

Run: `dotnet build`
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 3: Visual verification**

In the running app while logged in:
- Inactive tabs ("משחק" when on GamesList, etc.) render slightly faded.
- Hovering an inactive tab brings it to full opacity with an underline.
- The tab for the current page shows a rounded translucent-white pill, bold, full opacity.
- The logout button behaves like an inactive tab (fades/raises on hover).

- [ ] **Step 4: Commit**

```bash
git add Client/Shared/NavMenu.razor.css
git commit -m "feat(client): tab opacity states and active-tab pill in nav menu"
```

---

### Task 4: MyButton — hover states for primary and secondary

**Files:**
- Modify: `Client/Components/MyButton.razor.css`

- [ ] **Step 1: Add hover rules**

In `Client/Components/MyButton.razor.css`, insert a hover rule after the `.my-btn.primary` block (after line 20) and another after the `.my-btn.secondary` block (after line 38):

```css
/* ריחוף על הכפתור הראשי - כתום כהה יותר לחיווי לחיצוּת */
.my-btn.primary:hover {
    background-color: #e67e22;
}
```

```css
/* ריחוף על הכפתור המשני - גוון כתום בהיר עדין ברקע */
.my-btn.secondary:hover {
    background-color: #fff4e8;
}
```

The `.my-btn.disabled` rule stays untouched — it never gets a hover effect because the `disabled` class replaces `primary`/`secondary` entirely (see `MyButton.razor:5,12`), so the hover selectors above cannot match a disabled button.

- [ ] **Step 2: Build**

Run: `dotnet build`
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 3: Visual verification**

In the QuestionsEditor add-question row:
- "אישור" (primary) darkens on hover; pressing still sinks it 5px.
- "ביטול" / "+ ליצור שאלה" (secondary) gains a light orange tint on hover.
- While a question is being added, "+ ליצור שאלה" is disabled (gray) and shows no hover change, only `not-allowed` cursor.

- [ ] **Step 4: Commit**

```bash
git add Client/Components/MyButton.razor.css
git commit -m "feat(client): hover states for primary and secondary buttons"
```

---

### Task 5: Dummy worm in the add-question row — give it the Silky structure styles

**Why:** The add-question dummy row (`QuestionsEditor.razor:49-97`) reuses the class names `segment-wrapper`, `label-container`, `silky-head`, and `placeholder-segment`, but those are defined only in `Silky.razor.css`, and CSS isolation means they do **not** apply outside the Silky component. The dummy worm currently renders unstyled. Copying the structural rules (including the new resting opacity from Task 1) makes the dummy match the reference layout.

**Files:**
- Modify: `Client/Pages/QuestionsEditor.razor.css` (append new rules)

- [ ] **Step 1: Append the structural rules**

Append to `Client/Pages/QuestionsEditor.razor.css`:

```css
/* עיצוב תולעת הדמה בשורת הוספת שאלה - העתק של המבנה מ-Silky.razor.css,
   כי בידוד ה-CSS של בלייזור לא מחיל את הכללים של הקומפוננטה על העמוד הזה */
.segment-wrapper {
    display: flex;
    flex-direction: column;
    align-items: center;
    align-self: flex-start;
}

.label-container {
    min-height: 32px; /* שומר על גובה אחיד גם כשאין תגית */
    margin-bottom: 10px;
    display: flex;
    justify-content: center;
}

.silky-head {
    display: flex;
    width: 87px;
    height: 87px;
    justify-content: center;
    align-items: center;
    background-image: url('./svg/silkyHead.svg');
    background-size: contain;
    background-repeat: no-repeat;
    background-position: center;
    z-index: 2;
}

.placeholder-segment {
    width: 100px;
    height: auto;
    margin-left: 5px;
    opacity: 0.55; /* אותו מצב מנוחה כמו בתולעת האמיתית */
}
```

No highlight class here — the dummy row is wrapped in `.disabled-area` (opacity 0.4, `pointer-events: none`), so it never reacts to hover, which matches the reference's blocked preview state.

- [ ] **Step 2: Build**

Run: `dotnet build`
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 3: Visual verification**

Click "+ ליצור שאלה":
- The dummy row shows a properly sized worm head (87px) with two faded dashed segments beside it, each with its edge label above, all grayed out by `.disabled-area` with a blocked cursor.

- [ ] **Step 4: Commit**

```bash
git add Client/Pages/QuestionsEditor.razor.css
git commit -m "feat(client): style dummy worm in add-question row to match Silky"
```

---

### Task 6: One editor screen — `GameEditor` host page with tabs and back button

**Why:** The reference app shows "עריכת השאלות" and "הגדרות כלליות" as tab-buttons on the *same screen/URL*. Today they are two separately routed pages. This task creates a routable host page and demotes both pages to child components. The old routes `/QuestionsEditor/{id}` and `/GeneralSettings/{id}` stop existing; everything lives at `/GameEditor/{id}`.

**Files:**
- Create: `Client/Pages/GameEditor.razor`
- Create: `Client/Pages/GameEditor.razor.css`
- Modify: `Client/Pages/QuestionsEditor.razor:1-4`
- Modify: `Client/Pages/GeneralSettings.razor:1-13` and `SaveSettingsAndMove` (around line 138)
- Modify: `Client/Pages/GamesList.razor:201,293`

- [ ] **Step 1: Create the host page**

Create `Client/Pages/GameEditor.razor` (same folder as the two child components, so no `@using` is needed — Blazor gives files in `Pages/` the same namespace):

```razor
@page "/GameEditor/{gameID:int}"
@* עמוד מארח שמאגד את עריכת השאלות ואת ההגדרות הכלליות תחת כתובת אחת עם טאבים *@
@attribute [Authorize]

<div class="editor-header">
    @* שני הטאבים של מסך העריכה - בפריסת RTL האלמנט הראשון יושב בצד ימין *@
    <div class="editor-tabs">
        <button type="button" class="editor-tab @(activeTab == "settings" ? "active-tab" : "")"
                @onclick="@(() => SwitchTab("settings"))">
            ⚙️ הגדרות כלליות
        </button>
        <button type="button" class="editor-tab @(activeTab == "questions" ? "active-tab" : "")"
                @onclick="@(() => SwitchTab("questions"))">
            ✏️ עריכת השאלות
        </button>
    </div>

    @* כפתור החזרה לטבלת המשחקים - יושב בפינה השמאלית *@
    <a class="back-pill" href="./GamesList">טבלת המשחקים <span class="back-arrow">←</span></a>
</div>

@* התוכן של הטאב הפעיל *@
@if (activeTab == "questions")
{
    <QuestionsEditor gameID="gameID"/>
}
else
{
    <GeneralSettings gameID="gameID"/>
}

@* כיתוב הסטטוס הקבוע בתחתית המסך (יהפוך דינמי בטאסק הבא) *@
<p class="status-caption">השינויים נשמרים אוטומטית 💡</p>

@code {
    // הפרמטר שמתקבל מהנתיב (מזהה המשחק)
    [Parameter] public int gameID { get; set; }

    // טאב פתיחה אופציונלי מכתובת ה-URL, למשל ?tab=settings אחרי יצירת משחק חדש
    [Parameter, SupplyParameterFromQuery(Name = "tab")]
    public string startTab { get; set; }

    // הטאב הפעיל כרגע - ברירת המחדל היא עריכת השאלות
    string activeTab = "questions";

    protected override void OnInitialized()
    {
        // רק בכניסה לעמוד - בחירת הטאב לפי הכתובת (החלפות טאב בהמשך הן מקומיות בלבד)
        if (startTab == "settings")
        {
            activeTab = "settings";
        }
    }

    // החלפת טאב מקומית - הכתובת בדפדפן לא משתנה, כמו באפליקציית הייחוס
    void SwitchTab(string tabName)
    {
        activeTab = tabName;
    }
}
```

- [ ] **Step 2: Create the host page styles**

Create `Client/Pages/GameEditor.razor.css`:

```css
/* שורת הכותרת העליונה - הטאבים בצד ימין וכפתור החזרה בצד שמאל (הפריסה כולה RTL) */
.editor-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 15px 20px;
}

/* כפתור החזרה לטבלת המשחקים בעיצוב גלולה לבנה */
.back-pill {
    background-color: white;
    color: #333;
    font-weight: bold;
    text-decoration: none;
    border-radius: 20px;
    padding: 8px 18px;
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
    opacity: 0.9; /* מצב מנוחה */
    transition: opacity 0.2s ease, box-shadow 0.2s ease;
}

/* ריחוף על כפתור החזרה - שקיפות מלאה והצללה מודגשת */
.back-pill:hover {
    opacity: 1;
    box-shadow: 0 3px 8px rgba(0, 0, 0, 0.25);
}

/* החץ הכתום בתוך כפתור החזרה */
.back-arrow {
    color: #fd8a03;
    font-weight: bold;
}

/* מכל הטאבים */
.editor-tabs {
    display: flex;
    gap: 12px;
}

/* טאב במצב מנוחה - לבן, מעוגל, בשקיפות מופחתת */
.editor-tab {
    background-color: white;
    border: 2px solid transparent;
    border-radius: 12px;
    padding: 10px 18px;
    font-size: 16px;
    font-weight: bold;
    cursor: pointer;
    opacity: 0.8;
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.12);
    transition: opacity 0.2s ease, border-color 0.2s ease;
}

/* ריחוף על טאב - שקיפות מלאה */
.editor-tab:hover {
    opacity: 1;
}

/* הטאב הפעיל - שקיפות מלאה ומסגרת כתומה */
.editor-tab.active-tab {
    opacity: 1;
    border-color: #fd8a03;
}

/* כיתוב הסטטוס בתחתית המסך */
.status-caption {
    text-align: center;
    color: #555;
    font-size: 14px;
    margin-top: 20px;
}
```

- [ ] **Step 3: Demote `QuestionsEditor` to a child component**

In `Client/Pages/QuestionsEditor.razor`, replace lines 1–4:

```razor
@page "/QuestionsEditor/{gameID:int}"
<h1>עריכת שאלות</h1>
<!-- ניהול משתמשים - רק יוזר מחובר יכול להיכנס -->
@attribute [Authorize]
```

with:

```razor
@* קומפוננטת עריכת השאלות - מוצגת בתור טאב בתוך העמוד המארח GameEditor *@
@* ההרשאות (Authorize) נאכפות בעמוד המארח, והכותרת עברה לשם *@
```

The `[Parameter] public int gameID` already exists (line 277) — as a child component it now receives the value from `GameEditor` instead of from the route.

- [ ] **Step 4: Demote `GeneralSettings` to a child component**

In `Client/Pages/GeneralSettings.razor`, replace lines 1–13:

```razor
@page "/GeneralSettings/{gameID:int}"
<!-- ניהול משתמשים - רק יוזר מחובר יכול להיכנס -->
@attribute [Authorize]
@inject HttpClient Http
@using AuthTemplate.Shared.Models;
@inject NavigationManager Nav;

@* המכל הראשי שמכיל את הדשא *@
<div class="settings-page-wrapper">
   
    
<a href="./GamesList">בחזרה למשחקים שלי</a>
<h1>⚙️הגדרות כלליות️</h1>
```

with:

```razor
@* קומפוננטת ההגדרות הכלליות - מוצגת בתור טאב בתוך העמוד המארח GameEditor *@
@* ההרשאות, קישור החזרה והטאבים נמצאים בעמוד המארח *@
@inject HttpClient Http
@using AuthTemplate.Shared.Models;
@inject NavigationManager Nav;

@* המכל הראשי שמכיל את הדשא *@
<div class="settings-page-wrapper">

<h1>⚙️הגדרות כלליות️</h1>
```

Then in `SaveSettingsAndMove` (around line 138), change the post-save navigation target from the removed route to the new host page:

```csharp
            msg = "השינויים נשמרו בהצלחה!";
            Nav.NavigateTo("./GameEditor/" + gameID.ToString());
```

(Task 7 replaces this navigation with an event; this keeps the app working within this task.)

- [ ] **Step 5: Update the navigation targets in `GamesList`**

In `Client/Pages/GamesList.razor` line 201 (after creating a new game — opens the settings tab first, like the old flow):

```csharp
            Nav.NavigateTo("./GameEditor/" + newGame.gameID.ToString() + "?tab=settings");
```

And line 293 (`EditGame`):

```csharp
        Nav.NavigateTo("/GameEditor/" + game.gameID);
```

- [ ] **Step 6: Build**

Run: `dotnet build`
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 7: Visual verification**

- From GamesList, click a game's edit action → lands on `/GameEditor/{id}`, questions tab active (orange border, full opacity), settings tab faded, hover raises it.
- Click "הגדרות כלליות" → settings content swaps in, URL unchanged.
- Create a new game → lands on `/GameEditor/{id}?tab=settings` with the settings tab active.
- "טבלת המשחקים ←" pill (top-left) navigates back to GamesList; hover raises opacity/shadow.
- Saving settings navigates to `/GameEditor/{id}` (questions tab).

- [ ] **Step 8: Commit**

```bash
git add Client/Pages/GameEditor.razor Client/Pages/GameEditor.razor.css Client/Pages/QuestionsEditor.razor Client/Pages/GeneralSettings.razor Client/Pages/GamesList.razor
git commit -m "feat(client): merge questions editor and settings into tabbed GameEditor page"
```

---

### Task 7: Status caption — "השינויים נשמרים אוטומטית" ↔ "השינויים נשמרו בהצלחה"

**Why:** Screenshots 1–2 show the resting caption "השינויים נשמרים אוטומטית" at the bottom center; screenshot 3 shows it switching to "השינויים נשמרו בהצלחה" after a successful item add. Children report successful server saves to the host via an `EventCallback`; the host swaps the caption and reverts after a few seconds (same `Task.Delay` pattern the codebase already uses for its toast).

**Files:**
- Modify: `Client/Pages/GameEditor.razor` (caption becomes dynamic, handlers added)
- Modify: `Client/Pages/QuestionsEditor.razor` (new `OnChangesSaved` parameter + 7 invocation points)
- Modify: `Client/Pages/GeneralSettings.razor` (new `OnSettingsSaved` parameter replaces post-save navigation)

- [ ] **Step 1: Make the caption dynamic in the host**

In `Client/Pages/GameEditor.razor`, replace the static caption line:

```razor
<p class="status-caption">השינויים נשמרים אוטומטית 💡</p>
```

with:

```razor
<p class="status-caption">@statusMessage</p>
```

Replace the two child tags with callback-wired versions:

```razor
@if (activeTab == "questions")
{
    <QuestionsEditor gameID="gameID" OnChangesSaved="ShowSavedMessage"/>
}
else
{
    <GeneralSettings gameID="gameID" OnSettingsSaved="SettingsSaved"/>
}
```

And add to the `@code` block:

```csharp
    // כיתוב ברירת המחדל בתחתית המסך
    const string autoSaveText = "השינויים נשמרים אוטומטית 💡";
    string statusMessage = "השינויים נשמרים אוטומטית 💡";

    // מציג את הודעת ההצלחה אחרי שמירה מוצלחת, ומחזיר את כיתוב ברירת המחדל אחרי כמה שניות
    async Task ShowSavedMessage()
    {
        statusMessage = "השינויים נשמרו בהצלחה ✨";
        StateHasChanged();

        await Task.Delay(4000);

        statusMessage = autoSaveText;
        StateHasChanged();
    }

    // אחרי שמירת ההגדרות - מציגים את ההודעה וחוזרים לטאב עריכת השאלות (כמו הניווט הישן)
    async Task SettingsSaved()
    {
        activeTab = "questions";
        await ShowSavedMessage();
    }
```

- [ ] **Step 2: Report saves from `QuestionsEditor`**

In `Client/Pages/QuestionsEditor.razor`, add the parameter next to the existing `gameID` parameter (line 277):

```csharp
    // פעמון שמדווח לעמוד המארח על שמירה מוצלחת, כדי שיציג "השינויים נשמרו בהצלחה"
    [Parameter] public EventCallback OnChangesSaved { get; set; }
```

Add `await OnChangesSaved.InvokeAsync();` at these 7 success points (each line goes immediately after the quoted existing line):

1. `ConfirmNewQuestion` — after `currentStrawberryItem = new SilkyItem();` (line ~398, inside the success branch).
2. `ConfirmQuestionDelete` — after `gameData.Questions.Remove(questionToDelete);` (line ~425).
3. `ExecuteDeletion` — after the `foreach` loop that removes the item, right before `errorMsg = "";` (line ~517).
4. `UpdateItemInServer` — after `currentStrawberryItem = new SilkyItem();` (line ~644, success branch).
5. `AddNewItemToServer` — after `currentStrawberryItem = new SilkyItem();` (line ~736, success branch).
6. `SaveItemOrderToServer` — inside the `else` (success) branch, after `errorMsg = "";` (line ~805). Note: a reorder calls this twice (once per swapped item), so the caption simply refreshes — acceptable.
7. `UpdateQuestionInServer` — inside the `else` (success) branch, after `successMsg = "הקצה נשמר בהצלחה!";` (line ~705).

All 7 methods are already `async Task`, so `await` compiles without signature changes.

- [ ] **Step 3: Report saves from `GeneralSettings`**

In `Client/Pages/GeneralSettings.razor`, add below the `gameID` parameter (line ~102):

```csharp
    // פעמון שמדווח לעמוד המארח על שמירה מוצלחת של ההגדרות
    [Parameter] public EventCallback OnSettingsSaved { get; set; }
```

In `SaveSettingsAndMove`, replace the success branch:

```csharp
        if (updateResponse.IsSuccessStatusCode == true)
        {
            msg = "השינויים נשמרו בהצלחה!";
            Nav.NavigateTo("./GameEditor/" + gameID.ToString());
        }
```

with:

```csharp
        if (updateResponse.IsSuccessStatusCode == true)
        {
            // מדווחים לעמוד המארח - הוא יציג את ההודעה בתחתית ויעבור לטאב עריכת השאלות
            await OnSettingsSaved.InvokeAsync();
        }
```

(The `@inject NavigationManager Nav` line can stay — removing it is optional cleanup.)

- [ ] **Step 4: Build**

Run: `dotnet build`
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 5: Visual verification**

- Open `/GameEditor/{id}` → bottom caption reads "השינויים נשמרים אוטומטית 💡".
- Add an item to a question → caption switches to "השינויים נשמרו בהצלחה ✨" (matches screenshot 3), reverts after ~4 seconds.
- Delete an item, reorder items, edit an edge label, edit an instruction → each successful save shows the message.
- In the settings tab, click "שמירה" → view switches to the questions tab and the success caption shows.

- [ ] **Step 6: Commit**

```bash
git add Client/Pages/GameEditor.razor Client/Pages/QuestionsEditor.razor Client/Pages/GeneralSettings.razor
git commit -m "feat(client): autosave status caption driven by child save events"
```

---

## Out of Scope

- The reference app's leaf artwork (this app deliberately uses the strawberry/worm art instead).
- Character-counter colors (already implemented in `CustomInputText`/`CustomInputTextArea` via `midLength`).
- Item-count column update on successful add (already works — `AddNewItemToServer` adds to `question.Items`).
- Accordion row hover, arrow buttons, action icons — they already have hover states matching the reference.
