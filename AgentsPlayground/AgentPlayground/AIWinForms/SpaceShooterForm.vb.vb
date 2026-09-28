Imports System.Windows.Forms
Imports System.Drawing
Imports System.Reflection

''' <summary>
''' Space Shooter - A thin canvas form that delegates all game logic to GameEngine.
''' Uses high-performance double-buffered rendering for responsive gameplay.
''' </summary>
Public Class SpaceShooterForm
    Inherits Form

    ' ===== Canvas and timing =====
    Private paintTimer As System.Windows.Forms.Timer
    Private Const TICK_INTERVAL_MS As Integer = 16 ' ~60 FPS game loop

    ' ===== Game engine (owns all state and logic) =====
    Private engine As New GameEngine()
    Private imageCache As Dictionary(Of String, Image) = New Dictionary(Of String, Image)()

    ' ===== Loaded images =====
    Private imgStars As Image = Nothing
    Private imgPlayerShip As Image = Nothing
    Private imgEnemy As Image = Nothing
    Private imgBeamBlue As Image = Nothing


    Public Sub New()
        InitializeComponent()
    End Sub


    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)

        ' Load embedded resource images lazily
        LoadImages()

        ' Initialize the engine
        engine.SurfaceWidth = gamePanel.Width
        engine.SurfaceHeight = gamePanel.Height

        ' Wire up game over callback
        engine.OnGameOver = Sub(finalScore)
            Invoke(Sub()
                statusText.Text = "GAME OVER! Click Restart to play again."
                restartButton.Visible = True
            End Sub)
        End Sub

        ' Start the high-performance game loop
        InitializeGameLoop()
    End Sub


    Private Sub LoadImages()
        Dim asm As Assembly = Assembly.GetExecutingAssembly()
        Dim names() As String = asm.GetManifestResourceNames()

        For Each fileName In {"stars.png", "starfighter.png", "XWing.png", "beamBlue.png"}
            For Each res In names
                If res.EndsWith(fileName, StringComparison.OrdinalIgnoreCase) Then
                    Using stream As IO.Stream = asm.GetManifestResourceStream(res)
                        If stream IsNot Nothing Then
                            imageCache(fileName.ToLower()) = New Bitmap(stream)
                            Exit For
                        End If
                    End Using
                End If
            Next
        Next

        ' Cache references
        If imageCache.ContainsKey("stars.png") Then imgStars = imageCache("stars.png")
        If imageCache.ContainsKey("starfighter.png") Then imgPlayerShip = imageCache("starfighter.png")
        If imageCache.ContainsKey("xwing.png") Then imgEnemy = imageCache("XWing.png")
        If imageCache.ContainsKey("beamblue.png") Then imgBeamBlue = imageCache("beamBlue.png")
    End Sub


    Private Sub InitializeGameLoop()
        ' Create a timer for the game loop - uses system timer for responsiveness
        paintTimer = New System.Windows.Forms.Timer()
        paintTimer.Interval = TICK_INTERVAL_MS
        AddHandler paintTimer.Tick, AddressOf GameTick
        paintTimer.Start()

        engine.StartGame()
    End Sub


    ''' <summary>Called ~60 times per second by the game timer.</summary>
    Private Sub GameTick(sender As Object, e As EventArgs)
        ' Calculate delta time for smooth movement regardless of frame rate
        Dim now As Double = Environment.TickCount64 / 1000.0
        Static lastTime As Double = now

        Dim delta As Double = now - lastTime
        lastTime = now

        ' Clamp delta to prevent spiral of death on lag spikes
        If delta > 0.1 Then delta = 0.016

        engine.Update(delta)
        gamePanel.Invalidate()
    End Sub


    ''' <summary>Draw the current frame using the renderer.</summary>
    Private Sub gamePanel_Paint(sender As Object, e As PaintEventArgs) Handles gamePanel.Paint
        Dim g As Graphics = e.Graphics
        ' Enable double buffering to prevent flicker
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.InterpolationMode = InterpolationMode.HighQualityBilinear

        ' Apply starfield image if available
        engine.StarImage = imgStars

        ' Render through the dedicated renderer class (pure rendering, no logic)
        SpaceShooterRenderer.Render(g, engine)

        g.Dispose()
    End Sub


    ''' <summary>Enable double buffering on the paint panel to prevent flicker.</summary>
    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.Style = cp.Style Or &H02000000 ' WS_CLIPCHILDREN for better painting
            Return cp
        End Get
    End Property


    ' ===== Keyboard input - forwarded directly to the engine =====
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If engine IsNot Nothing Then
            engine.KeyDown(e.KeyCode)
        End If
        e.SuppressKeyPress = True
    End Sub


    Protected Overrides Sub OnKeyUp(e As KeyEventArgs)
        MyBase.OnKeyUp(e)
        If engine IsNot Nothing Then
            engine.KeyUp(e.KeyCode)
        End If
        e.SuppressKeyPress = True
    End Sub


    ''' <summary>Update the panel size to match the game surface.</summary>
    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        If engine IsNot Nothing Then
            engine.SurfaceWidth = gamePanel.Width
            engine.SurfaceHeight = gamePanel.Height
        End If
        gamePanel.Invalidate()
    End Sub


    Private Sub restartButton_Click(sender As Object, e As EventArgs) Handles restartButton.Click
        restartButton.Visible = False
        engine.StartGame()
    End Sub


    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        If paintTimer IsNot Nothing Then
            paintTimer.Stop()
            paintTimer.Dispose()
        End If
        If engine IsNot Nothing Then
            engine.StopGame()
            engine.Dispose()
        End If
    End Sub

End Class
