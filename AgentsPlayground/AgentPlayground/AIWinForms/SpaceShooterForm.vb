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
    Private imgPlayer As Image = Nothing
    Private imgEnemy As Image = Nothing
    Private imgBullet As Image = Nothing
    Private _loadedImages As Boolean = False

    ''' <summary>
    ''' Loads an embedded resource image and converts it to a pixel format GDI+ can
    ''' blit fast. Creating a Bitmap directly over the resource stream leaves it in a
    ''' raw format, which forces per-draw pixel conversion, so it is copied here.
    ''' </summary>
    Private Function LoadEmbeddedImage(fileName As String) As Image
        Dim asm As Assembly = Assembly.GetExecutingAssembly()
        For Each res In asm.GetManifestResourceNames()
            If res.EndsWith(fileName, StringComparison.OrdinalIgnoreCase) Then
                Using stream As IO.Stream = asm.GetManifestResourceStream(res)
                    If stream Is Nothing Then Return Nothing
                    Using raw As Image = Image.FromStream(stream)
                        Dim bmp As New Bitmap(raw.Width, raw.Height, PixelFormat.Format32bppPArgb)
                        Using g As Graphics = Graphics.FromImage(bmp)
                            g.InterpolationMode = InterpolationMode.NearestNeighbor
                            g.PixelOffsetMode = PixelOffsetMode.Half
                            g.DrawImage(raw, 0, 0, raw.Width, raw.Height)
                        End Using
                        Return bmp
                    End Using
                End Using
            End If
        Next
        Return Nothing
    End Function

    ''' <summary>Loads all game images once and wires them into the engine.</summary>
    Private Sub LoadAllImages()
        If _loadedImages Then Exit Sub

        imgStars = LoadEmbeddedImage("stars.png")
        imgPlayer = LoadEmbeddedImage("starfighter.png")
        imgEnemy = LoadEmbeddedImage("XWing.png")
        imgBullet = LoadEmbeddedImage("beamBlue.png")

        If IsValidImage(imgStars) Then engine.StarsImage = imgStars
        If IsValidImage(imgPlayer) Then engine.PlayerImage = imgPlayer
        If IsValidImage(imgEnemy) Then engine.EnemyImage = imgEnemy
        If IsValidImage(imgBullet) Then engine.BulletImage = imgBullet

        ' Sprites changed, so any pre-scaled sprite / star-strip cache is stale.
        SpaceShooterRenderer.ResetCaches()
        _loadedImages = True
    End Sub

    ''' <summary>Checks whether an image is a valid non-placeholder bitmap.</summary>
    Private Function IsValidImage(img As Image) As Boolean
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
        EnsureBackBuffer()

        engine.PlayerX = (engine.SurfaceWidth - GameEngine.PLAYER_WIDTH) / 2.0
        engine.PlayerY = engine.SurfaceHeight - GameEngine.PLAYER_HEIGHT - 40
        engine.StartGame()

        gameOverState = False
        UpdateStatusLabels(True)
        restartButton.Visible = False

        RenderAndBlit()   ' immediate first frame display (no async paint needed)
    End Sub

    Private Sub EnsureBackBuffer()
        If gamePanel Is Nothing Then Return
        ' ClientSize, not Width/Height: gamePanel has a FixedSingle border, and
        ' CreateGraphics paints into the client area only. Using Width/Height would
        ' build a backbuffer a couple of pixels larger than the drawable region.
        Dim w = gamePanel.ClientSize.Width
        Dim h = gamePanel.ClientSize.Height
        If w <= 0 OrElse h <= 0 Then Return

        ' The engine's surface size drives the star strip and HUD layout, so it has
        ' to follow the real panel size (DPI scaling changes it from the design size).
        If engine.SurfaceWidth <> w OrElse engine.SurfaceHeight <> h Then
            engine.SurfaceWidth = w
            engine.SurfaceHeight = h
        End If

        If backBuffer Is Nothing OrElse backBuffer.Width <> w OrElse backBuffer.Height <> h Then
            If offscreenGraphics IsNot Nothing Then
                offscreenGraphics.Dispose()
                offscreenGraphics = Nothing
            End If
            If backBuffer IsNot Nothing Then
                gamePanel.Frame = Nothing
                backBuffer.Dispose()
                backBuffer = Nothing
            End If
            Try
                backBuffer = New Bitmap(w, h, PixelFormat.Format32bppPArgb)
                offscreenGraphics = Graphics.FromImage(backBuffer)
                ' The panel restores this bitmap on any framework repaint, so resize,
                ' minimize/restore or a sibling control appearing never blanks the game.
                gamePanel.Frame = backBuffer
            Catch
                offscreenGraphics = Nothing
                backBuffer = Nothing
                gamePanel.Frame = Nothing
            End Try

            ' The pre-composited star strip is sized to the surface, so rebuild it.
            SpaceShooterRenderer.ResetCaches()
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

        UpdateStatusLabels(False)
        RenderAndBlit()   ' render + blit in one go
    End Sub

    ''' <summary>Updates the score/status labels only when their text actually changes.</summary>
    Private Sub UpdateStatusLabels(force As Boolean)
        Dim scoreText = $"Score: {engine.Score}  |  Lives: {engine.Lives}"
        If force OrElse scoreText <> lastScoreText Then
            lastScoreText = scoreText
            scoreLabel.Text = scoreText
        End If

        If Not gameOverState AndAlso engine.GameOver Then
            gameOverState = True
            statusText.Text = "GAME OVER!"
            restartButton.Visible = True
        ElseIf gameOverState AndAlso Not engine.GameOver Then
            gameOverState = False
            statusText.Text = "Arrow Keys/WASD to Move — Space to Shoot"
            restartButton.Visible = False
        End If
    End Sub

    ' ================================================================
    '  Render: draw everything into backbuffer, then blit onto panel
    ' ================================================================
    Private Sub RenderAndBlit()
        EnsureBackBuffer()
        If backBuffer Is Nothing OrElse offscreenGraphics Is Nothing Then Return

        SpaceShooterRenderer.Render(offscreenGraphics, engine)
        offscreenGraphics.Flush()   ' push GDI+ batching before the blit

        DirectBlit()
    End Sub

    ''' <summary>Copies the completed backbuffer onto the game surface in one blit.</summary>
    Private Sub DirectBlit()
        If gamePanel Is Nothing OrElse backBuffer Is Nothing Then Return
        If Not gamePanel.IsHandleCreated Then Return
        If gamePanel.ClientSize.Width < backBuffer.Width OrElse
           gamePanel.ClientSize.Height < backBuffer.Height Then Return

        Using g As Graphics = gamePanel.CreateGraphics()
            g.DrawImageUnscaled(backBuffer, 0, 0)
        End Using
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
        If offscreenGraphics IsNot Nothing Then offscreenGraphics.Dispose()
        If backBuffer IsNot Nothing Then
            gamePanel.Frame = Nothing
            backBuffer.Dispose()
            backBuffer = Nothing
        End If
        ' Cached fonts/brushes/sprites in the renderer are process-static: release
        ' them so reopening the form rebuilds rather than reusing stale bitmaps.
        SpaceShooterRenderer.ResetCaches()

        ' Clear the engine's sprite references too, so nothing can draw with a
        ' disposed image if a frame is rendered during teardown.
        engine.StarsImage = Nothing
        engine.PlayerImage = Nothing
        engine.EnemyImage = Nothing
        engine.BulletImage = Nothing
        _loadedImages = False

        If imgStars IsNot Nothing Then imgStars.Dispose()
        If imgPlayer IsNot Nothing Then imgPlayer.Dispose()
        If imgEnemy IsNot Nothing Then imgEnemy.Dispose()
        If imgBullet IsNot Nothing Then imgBullet.Dispose()
        imgStars = Nothing
        imgPlayer = Nothing
        imgEnemy = Nothing
        imgBullet = Nothing
    End Sub

    ' ===== Keys we poll every tick =====
    Private ReadOnly keyStates As New Dictionary(Of Keys, Boolean)()
    Private Shared ReadOnly KeySet As New List(Of Keys) From {
        Keys.Up, Keys.Down, Keys.Left, Keys.Right,
        Keys.W, Keys.A, Keys.S, Keys.D,
        Keys.Space
    }

End Class