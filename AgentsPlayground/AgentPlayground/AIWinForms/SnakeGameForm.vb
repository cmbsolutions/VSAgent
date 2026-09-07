Public Class SnakeGameForm
    Private snake As New List(Of Point)()
    Private food As Point
    Private direction As String = "Right"
    Private nextDirection As String = "Right"
    Private score As Integer = 0
    Private gameRunning As Boolean = False
    Private rnd As New Random()

    Public Sub New()
        InitializeComponent()
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        InitializeGame()
    End Sub

    Private Sub InitializeGame()
        Dim centerX As Integer = CInt(gamePanel.Width / 2 / 20) * 20
        Dim centerY As Integer = CInt(gamePanel.Height / 2 / 20) * 20

        snake.Clear()
        ' Head first (index 0) — leading edge with room to move in current direction
        snake.Add(New Point(centerX, centerY))
        ' Body extends behind the head
        For i As Integer = 1 To 4 Step 1
            snake.Add(New Point(centerX - i * 20, centerY))
        Next

        direction = "Right"
        nextDirection = "Right"
        score = 0
        gameRunning = True

        scoreLabel.Text = "Score: 0"
        statusText.Text = "Use Arrow Keys to Control the Snake"
        restartButton.Visible = False
        gameTimer.Interval = 120
        gameTimer.Start()

        SpawnFood()
        gamePanel.Invalidate()
    End Sub

    Private Sub SpawnFood()
        Dim attempts As Integer = 0

        Do
            food.X = rnd.Next(0, CInt(gamePanel.Width / 20)) * 20
            food.Y = rnd.Next(0, CInt(gamePanel.Height / 20)) * 20
            attempts += 1
        Loop While snake.Contains(food) AndAlso attempts < 100

        gamePanel.Invalidate()
    End Sub

    Private Sub gameTimer_Tick(sender As Object, e As EventArgs) Handles gameTimer.Tick
        If Not gameRunning Then Return

        direction = nextDirection

        Dim head As Point = snake.First()
        Dim newHead As Point

        Select Case direction
            Case "Up" : newHead = New Point(head.X, head.Y - 20)
            Case "Down" : newHead = New Point(head.X, head.Y + 20)
            Case "Left" : newHead = New Point(head.X - 20, head.Y)
            Case "Right" : newHead = New Point(head.X + 20, head.Y)
        End Select

        ' Check wall collision
        If newHead.X < 0 OrElse newHead.X >= gamePanel.Width _
           OrElse newHead.Y < 0 OrElse newHead.Y >= gamePanel.Height Then
            GameOver()
            Return
        End If

        ' Check self collision
        If snake.Skip(1).Any(Function(p) p.Equals(newHead)) Then
            GameOver()
            Return
        End If

        snake.Insert(0, newHead)

        ' Check food collision
        If newHead.Equals(food) Then
            score += 10
            scoreLabel.Text = $"Score: {score}"
            
            ' Speed up slightly
            If gameTimer.Interval > 50 Then
                gameTimer.Interval -= 5
            End If
            
            SpawnFood()
        Else
            snake.RemoveAt(snake.Count - 1)
        End If

        gamePanel.Invalidate()
    End Sub

    Private Sub GameOver()
        gameRunning = False
        gameTimer.Stop()
        statusText.Text = "Game Over!"
        restartButton.Visible = True
    End Sub

    Private Sub Form_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not gameRunning Then Return

        Select Case e.KeyCode
            Case Keys.Up
                If direction <> "Down" Then nextDirection = "Up"
            Case Keys.Down
                If direction <> "Up" Then nextDirection = "Down"
            Case Keys.Left
                If direction <> "Right" Then nextDirection = "Left"
            Case Keys.Right
                If direction <> "Left" Then nextDirection = "Right"
        End Select

        e.Handled = True
    End Sub

    Private Sub restartButton_Click(sender As Object, e As EventArgs) Handles restartButton.Click
        InitializeGame()
    End Sub

    Private Sub gamePanel_Paint(sender As Object, e As PaintEventArgs) Handles gamePanel.Paint
        Dim g As Graphics = e.Graphics
        g.Clear(Color.Black)

        ' Draw grid
        Dim pen As New Pen(Color.FromArgb(30, Color.White))
        For x As Integer = 0 To CInt(gamePanel.Width / 20) - 1
            g.DrawLine(pen, x * 20, 0, x * 20, gamePanel.Height)
        Next
        For y As Integer = 0 To CInt(gamePanel.Height / 20) - 1
            g.DrawLine(pen, 0, y * 20, gamePanel.Width, y * 20)
        Next

        ' Draw food
        Dim brush As New SolidBrush(Color.LimeGreen)
        g.FillEllipse(brush, food.X + 2, food.Y + 2, 16, 16)
        brush.Dispose()

        ' Draw snake
        For i As Integer = 0 To snake.Count - 1
            Dim sBrush As SolidBrush
            If i = 0 Then
                sBrush = New SolidBrush(Color.Red)
            Else
                sBrush = New SolidBrush(Color.ForestGreen)
            End If
            g.FillRectangle(sBrush, snake(i).X + 1, snake(i).Y + 1, 18, 18)
            sBrush.Dispose()
        Next

        pen.Dispose()
    End Sub
End Class
