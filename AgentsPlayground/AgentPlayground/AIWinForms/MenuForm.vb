Imports System.ComponentModel
Imports System.Reflection

''' <summary>
''' Main menu form that allows the user to navigate to other application forms.
''' </summary>
Public Class MenuForm
    Inherits Form

    Private Sub InitializeComponent()
        ' ====== Main form settings ======
        Me.Text = "AIWinForms - Main Menu"
        Me.Size = New Size(480, 360)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimumSize = New Size(400, 280)

        ' ====== Title Label ======
        Dim lblTitle As New Label()
        lblTitle.Text = "Welcome to AIWinForms"
        lblTitle.Font = New Font(Me.Font.FontFamily, 18F, FontStyle.Bold)
        lblTitle.AutoSize = False
        lblTitle.Size = New Size(460, 40)
        lblTitle.Location = New Point(10, 20)
        lblTitle.ForeColor = Color.FromArgb(25, 62, 129)
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        lblTitle.BackColor = Color.Transparent

        ' ====== Subtitle Label ======
        Dim lblSubtitle As New Label()
        lblSubtitle.Text = "Select a tool below to open its window:"
        lblSubtitle.Font = New Font(Me.Font.FontFamily, 10F)
        lblSubtitle.AutoSize = False
        lblSubtitle.Size = New Size(460, 30)
        lblSubtitle.Location = New Point(10, 65)
        lblSubtitle.ForeColor = Color.Gray
        lblSubtitle.TextAlign = ContentAlignment.MiddleCenter
        lblSubtitle.BackColor = Color.Transparent

        ' ====== Separator Line ======
        Dim separator As New Label()
        separator.Text = ""
        separator.BorderStyle = BorderStyle.FixedSingle
        separator.Size = New Size(460, 2)
        separator.Location = New Point(10, 105)
        separator.BackColor = Color.LightGray

        ' ====== Define menu items (as separate variables to avoid collection initializer issues) ======
        ' Menu item 1: OpenAI API Client
        Dim menuItem1Panel As New Panel()
        menuItem1Panel.Name = "btn_OpenAIClientForm"
        menuItem1Panel.Location = New Point(10, 130)
        menuItem1Panel.Size = New Size(460, 72)
        menuItem1Panel.BackColor = Color.White
        menuItem1Panel.BorderStyle = BorderStyle.FixedSingle
        menuItem1Panel.Cursor = Cursors.Hand

        Dim accentBar1 As New Label()
        accentBar1.Name = "accent_0"
        accentBar1.Text = ""
        accentBar1.Size = New Size(5, 72)
        accentBar1.Location = New Point(0, 0)
        accentBar1.BackColor = Color.FromArgb(48, 193, 76)
        accentBar1.BorderStyle = BorderStyle.None

        Dim icon1 As New Label()
        icon1.Name = "icon_0"
        icon1.Text = ""
        icon1.Size = New Size(24, 24)
        icon1.Location = New Point(18, CType((72 - 24) \ 2, Integer))
        icon1.BackColor = Color.FromArgb(48, 193, 76)
        icon1.BorderStyle = BorderStyle.None

        Dim title1 As New Label()
        title1.Name = "title_0"
        title1.Text = "OpenAI API Client"
        title1.Font = New Font(Me.Font.FontFamily, 12F, FontStyle.Bold)
        title1.AutoSize = False
        title1.Size = New Size(380, 24)
        title1.Location = New Point(52, CType((72 - 24) \ 2 - 6, Integer))
        title1.ForeColor = Color.FromArgb(37, 37, 38)
        title1.TextAlign = ContentAlignment.MiddleLeft
        title1.BackColor = Color.Transparent

        Dim desc1 As New Label()
        desc1.Name = "desc_0"
        desc1.Text = "Send prompts to OpenAI Chat Completions API and view conversation history."
        desc1.Font = New Font(Me.Font.FontFamily, 9F)
        desc1.AutoSize = False
        desc1.Size = New Size(380, CType(72 - 24 - 18, Integer))
        desc1.Location = New Point(52, CType((72 - 24) \ 2 + 6, Integer))
        desc1.ForeColor = Color.DimGray
        desc1.TextAlign = ContentAlignment.MiddleLeft
        desc1.BackColor = Color.Transparent

        Dim arrow1 As New Label()
        arrow1.Name = "arrow_0"
        arrow1.Text = "→"
        arrow1.Font = New Font(Me.Font.FontFamily, 14F)
        arrow1.AutoSize = False
        arrow1.Size = New Size(30, 30)
        arrow1.Location = New Point(420, CType((72 - 30) \ 2, Integer))
        arrow1.ForeColor = Color.FromArgb(48, 193, 76)
        arrow1.TextAlign = ContentAlignment.MiddleCenter
        arrow1.BackColor = Color.Transparent

        ' Click handler for menu item 1 (OpenAI API Client)
        AddHandler menuItem1Panel.Click, Sub(s As Object, e As EventArgs)
                                             Dim ft As Type = GetType(OpenAIClientForm)
                                             If ft IsNot Nothing Then
                                                 Dim instance As Form = CType(Activator.CreateInstance(ft), Form)
                                                 instance.Show()
                                             End If
                                         End Sub

        menuItem1Panel.Controls.AddRange({accentBar1, icon1, title1, desc1, arrow1})

        ' Menu item 2: JSON Parser
        Dim menuItem2Panel As New Panel()
        menuItem2Panel.Name = "btn_JSONParser"
        menuItem2Panel.Location = New Point(10, CType(130 + 72 + 15, Integer))
        menuItem2Panel.Size = New Size(460, 72)
        menuItem2Panel.BackColor = Color.White
        menuItem2Panel.BorderStyle = BorderStyle.FixedSingle
        menuItem2Panel.Cursor = Cursors.Hand

        Dim accentBar2 As New Label()
        accentBar2.Name = "accent_1"
        accentBar2.Text = ""
        accentBar2.Size = New Size(5, 72)
        accentBar2.Location = New Point(0, 0)
        accentBar2.BackColor = Color.FromArgb(32, 145, 210)
        accentBar2.BorderStyle = BorderStyle.None

        Dim icon2 As New Label()
        icon2.Name = "icon_1"
        icon2.Text = ""
        icon2.Size = New Size(24, 24)
        icon2.Location = New Point(18, CType((72 - 24) \ 2, Integer))
        icon2.BackColor = Color.FromArgb(32, 145, 210)
        icon2.BorderStyle = BorderStyle.None

        Dim title2 As New Label()
        title2.Name = "title_1"
        title2.Text = "JSON Parser"
        title2.Font = New Font(Me.Font.FontFamily, 12F, FontStyle.Bold)
        title2.AutoSize = False
        title2.Size = New Size(380, 24)
        title2.Location = New Point(52, CType((72 - 24) \ 2 - 6, Integer))
        title2.ForeColor = Color.FromArgb(37, 37, 38)
        title2.TextAlign = ContentAlignment.MiddleLeft
        title2.BackColor = Color.Transparent

        Dim desc2 As New Label()
        desc2.Name = "desc_1"
        desc2.Text = "Paste JSON text and view it as an interactive tree structure."
        desc2.Font = New Font(Me.Font.FontFamily, 9F)
        desc2.AutoSize = False
        desc2.Size = New Size(380, CType(72 - 24 - 18, Integer))
        desc2.Location = New Point(52, CType((72 - 24) \ 2 + 6, Integer))
        desc2.ForeColor = Color.DimGray
        desc2.TextAlign = ContentAlignment.MiddleLeft
        desc2.BackColor = Color.Transparent

        Dim arrow2 As New Label()
        arrow2.Name = "arrow_1"
        arrow2.Text = "→"
        arrow2.Font = New Font(Me.Font.FontFamily, 14F)
        arrow2.AutoSize = False
        arrow2.Size = New Size(30, 30)
        arrow2.Location = New Point(420, CType((72 - 30) \ 2, Integer))
        arrow2.ForeColor = Color.FromArgb(32, 145, 210)
        arrow2.TextAlign = ContentAlignment.MiddleCenter
        arrow2.BackColor = Color.Transparent

        ' Click handler for menu item 2 (JSON Parser)
        AddHandler menuItem2Panel.Click, Sub(s As Object, e As EventArgs)
                                             Dim ft As Type = GetType(Form1)
                                             If ft IsNot Nothing Then
                                                 Dim instance As Form = CType(Activator.CreateInstance(ft), Form)
                                                 instance.Show()
                                             End If
                                         End Sub

        menuItem2Panel.Controls.AddRange({accentBar2, icon2, title2, desc2, arrow2})

        ' ====== Hover / Click events for panels ======
        AddHandler menuItem1Panel.MouseEnter, Sub(s As Object, e As EventArgs)
                                                  DirectCast(s, Panel).BackColor = Color.FromArgb(245, 247, 250)
                                              End Sub
        AddHandler menuItem1Panel.MouseLeave, Sub(s As Object, e As EventArgs)
                                                  DirectCast(s, Panel).BackColor = Color.White
                                              End Sub

        AddHandler menuItem2Panel.MouseEnter, Sub(s As Object, e As EventArgs)
                                                  DirectCast(s, Panel).BackColor = Color.FromArgb(245, 247, 250)
                                              End Sub
        AddHandler menuItem2Panel.MouseLeave, Sub(s As Object, e As EventArgs)
                                                  DirectCast(s, Panel).BackColor = Color.White
                                              End Sub

        ' ====== Footer Label ======
        Dim footerY As Integer = 130 + (72 + 15) + (72 + 15) + 20
        Dim footerLabel As New Label()
        footerLabel.Text = "Click a button to open the corresponding form."
        footerLabel.Font = New Font(Me.Font.FontFamily, 8.5F)
        footerLabel.AutoSize = False
        footerLabel.Size = New Size(460, 20)
        footerLabel.Location = New Point(10, footerY)
        footerLabel.ForeColor = Color.LightGray
        footerLabel.TextAlign = ContentAlignment.MiddleCenter
        footerLabel.BackColor = Color.Transparent

        ' ====== Wire up all controls ======
        Me.Controls.Add(footerLabel)
        Me.Controls.Add(menuItem2Panel)
        Me.Controls.Add(menuItem1Panel)
        Me.Controls.AddRange({separator, lblSubtitle, lblTitle})
    End Sub

End Class