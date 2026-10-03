Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Reflection

''' <summary>
''' High-performance space shooter using the GameEngine for simulation and SpaceShooterRenderer for rendering.
''' </summary>
Public Class SpaceShooterForm
    Inherits Form

    ' ===== Embedded resource image cache =====
    Private imgStars As Image = Nothing
    Private _loadedImages As Boolean = False

    ''' <summary>Loads an embedded resource image by file name.</summary>
    Private Function LoadEmbeddedImage(fileName As String) As Image
        Dim asm As Assembly = Assembly.GetExecutingAssembly()
        Dim names() As String = asm.GetManifestResourceNames()
        For Each res In names
            If res.EndsWith(fileName, StringComparison.OrdinalIgnoreCase) Then
                Using stream As IO.Stream = asm.GetManifestResourceStream(res)
                    If stream IsNot Nothing Then
                        Return New Bitmap(stream)
                    End If
                End Using
            End If
        Next
        Return Nothing
    End Function

    ''' <summary>Loads all game images once and wires them into the engine.</summary>
    Private Sub LoadAllImages()
        If _loadedImages Then Return
        imgStars = LoadEmbeddedImage("stars.png")
        If IsValidImage(imgStars) Then engine.StarsImage = imgStars
        _loadedImages = True
    End Function

    ''' <summary>Checks whether an image is a valid non-placeholder bitmap.</summary>
    Private Shared Function IsValidImage(img As Image) As Boolean
        Return img IsNot Nothing AndAlso img.Width >= 16 AndAlso img.Height >= 16
    End Function

    ' ===== The game engine (owns ALL game state & simulation) =====
    Private ReadOnly engine As New GameEngine()

    ' ===== Off-screen backbuffer for double-buffered rendering =====
    Private backBuffer As Bitmap = Nothing

    ' ===== Simple gameOver flag to track transitions =====
    Private gameOverState As Boolean = False

    Public Sub New()
        InitializeComponent()
        Me.DoubleBuffered = False   ' we handle double-buffering manually via backbuffer
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        LoadAllImages()
        ResetGame()
    End Sub

    Private Sub ResetGame()
        engine.PlayerX = (engine.SurfaceWidth - GameEngine.PLAYER_WIDTH) / 2.0
        engine.PlayerY = engine.SurfaceHeight - GameEngine.PLAYER_HEIGHT - 40
        engine.StartGame()

        gameOverState = False
        scoreLabel.Text = $"Score: {0}  |  Lives: {3}"
        statusText.Text = "Arrow Keys/WASD to Move — Space to Shoot"
        restartButton.Visible = False

        EnsureBackBuffer()
        RenderAndBlit()   ' immediate first frame display (no async paint needed)
    End Sub

    Private Sub EnsureBackBuffer()
        If gamePanel Is Nothing Then Return
        Dim w = gamePanel.Width
        Dim h = gamePanel.Height
        If w <= 0 OrElse h <= 0 Then Return

        If backBuffer Is Nothing OrElse backBuffer.Width <> w OrElse backBuffer.Height <> h Then
            If backBuffer IsNot Nothing Then backBuffer.Dispose()
            Try
                backBuffer = New Bitmap(w, h, PixelFormat.Format32bppArgb)
            Catch
            End Try
        End If
    End Sub

    ' ================================================================
    '  Game loop — all rendering happens here (synchronous, no async paint)
    ' ================================================================
    Private Sub gameTimer_Tick(sender As Object, e As EventArgs) Handles gameTimer.Tick
        If engine Is Nothing Then Return

        ' --- Poll 9 keys we care about, send KeyDown/KeyUp only on state change ---
        For Each k In KeySet
            Dim down = (GetKeyState(CInt(k)) And &H8000S) <> 0
            Dim prev As Boolean
            If keyStates.TryGetValue(k, prev) AndAlso prev = down Then Continue For
            If down Then engine.KeyDown(k) Else engine.KeyUp(k)
            keyStates(k) = down
        Next

        ' --- Update at fixed timestep (16.667 ms = 60 TPS) ---
        engine.Update(1.0 / 60.0)

        scoreLabel.Text = $"Score: {engine.Score}  |  Lives: {engine.Lives}"

        If Not gameOverState AndAlso engine.GameOver Then
            gameOverState = True
            statusText.Text = "GAME OVER!"
            restartButton.Visible = True
        ElseIf gameOverState AndAlso Not engine.GameOver Then
            gameOverState = False
            statusText.Text = "Arrow Keys/WASD to Move — Space to Shoot"
            restartButton.Visible = False
        End If

        RenderAndBlit()   ' render + blit in one go
    End Sub

    ' ================================================================
    '  Render: draw everything into backbuffer, then blit onto panel
    ' ================================================================
    Private Sub RenderAndBlit()
        EnsureBackBuffer()
        If backBuffer Is Nothing Then Return

        Using gOffscreen = Graphics.FromImage(backBuffer)
            gOffscreen.SmoothingMode = SmoothingMode.AntiAlias
            gOffscreen.InterpolationMode = InterpolationMode.NearestNeighbor
            SpaceShooterRenderer.Render(gOffscreen, engine)
        End Using

        If gamePanel IsNot Nothing Then
            Using g = gamePanel.CreateGraphics()
                g.DrawImageUnscaled(backBuffer, 0, 0)
            End Using
        End If
    End Sub

    ' --- Win32 GetKeyState for raw keyboard state ---
    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function GetKeyState(vKey As Integer) As Short
    End Function

    ' ===== Keyboard input (supplementary — poll in timer tick is authoritative) =====
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        engine.KeyDown(e.KeyCode)
        e.SuppressKeyPress = True
    End Sub

    Protected Overrides Sub OnKeyUp(e As KeyEventArgs)
        MyBase.OnKeyUp(e)
        engine.KeyUp(e.KeyCode)
        e.SuppressKeyPress = True
    End Sub

    ' ===== Resize backbuffer when panel resizes =====
    Protected Overrides Sub OnSizeChanged(e As EventArgs)
        MyBase.OnSizeChanged(e)
        EnsureBackBuffer()   ' will recreate on next RenderAndBlit if needed
    End Sub

    Private Sub restartButton_Click(sender As Object, e As EventArgs) Handles restartButton.Click
        gameOverState = False
        ResetGame()
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
        gameTimer.Stop()
        engine.Dispose()
        If backBuffer IsNot Nothing Then backBuffer.Dispose()
    End Sub

    ' ===== Keys we poll every tick =====
    Private ReadOnly keyStates As New Dictionary(Of Keys, Boolean)()
    Private Shared ReadOnly KeySet As New List(Of Keys) From {
        Keys.Up, Keys.Down, Keys.Left, Keys.Right,
        Keys.W, Keys.A, Keys.S, Keys.D,
        Keys.Space
    }

End Class