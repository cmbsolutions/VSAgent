Imports System.Drawing.Drawing2D

''' <summary>
''' Tic Tac Toe game form where the user plays against an AI opponent.
''' </summary>
Public Class TicTacToeForm
    Inherits Form

    ' Game state
    Private board(2, 2) As Integer          ' 0 = empty, 1 = player (X), 2 = computer (O)
    Private isPlayerTurn As Boolean = True
    Private gameOver As Boolean = False
    Private playerSymbol As Char = "X"c
    Private computerSymbol As Char = "O"c

    ' UI controls
    Private boardPanel As Panel = Nothing
    Private statusLabel As Label = Nothing
    Private resetButton As Button = Nothing
    Private cells(2, 2) As Button

    ' AI scores
    Private Const PlayerScore As Integer = -10
    Private Const ComputerScore As Integer = 10
    Private Const TieScore As Integer = 0

    ''' <summary>
    ''' Represents a board coordinate for the minimax algorithm.
    ''' </summary>
    Private Structure BoardMove
        Public Sub New(r As Integer, c As Integer)
            Me.R = r
            Me.C = c
        End Sub
        Public ReadOnly Property R As Integer
        Public ReadOnly Property C As Integer
    End Structure

    Public Sub New()
        Me.SuspendLayout()
        InitializeComponent()
        ResetGame()
        Me.ResumeLayout(False)
    End Sub

    Private Sub InitializeComponent()
        ' ====== Main form settings ======
        Me.Text = "Tic Tac Toe — You (X) vs AI (O)"
        Me.Size = New Size(420, 560)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimumSize = New Size(380, 480)
        Me.BackColor = Color.FromArgb(15, 20, 35)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False

        ' ====== Title Label ======
        Dim titleLabel As New Label() With {
            .Text = "Tic Tac Toe",
            .Font = New Font("Segoe UI", 20.0F, FontStyle.Bold),
            .Location = New Point(110, 15),
            .Size = New Size(200, 35),
            .ForeColor = Color.FromArgb(0, 229, 255),
            .BackColor = Color.Transparent,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(titleLabel)

        ' ====== Subtitle / instruction ======
        Dim instrLabel As New Label() With {
            .Text = "You are X — go first!",
            .Font = New Font("Segoe UI", 9.5F),
            .Location = New Point(100, 52),
            .Size = New Size(220, 20),
            .ForeColor = Color.FromArgb(120, 160, 200),
            .BackColor = Color.Transparent,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(instrLabel)

        ' ====== Status label (shows turn / result) ======
        statusLabel = New Label() With {
            .Name = "statusLabel",
            .Text = "Your turn — click a cell",
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .Location = New Point(50, 85),
            .Size = New Size(320, 30),
            .ForeColor = Color.FromArgb(200, 220, 255),
            .BackColor = Color.Transparent,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(statusLabel)

        ' ====== Board panel with gradient background ======
        boardPanel = New Panel() With {
            .Name = "boardPanel",
            .Location = New Point(30, 125),
            .Size = New Size(360, 360),
            .BackColor = Color.FromArgb(22, 30, 50)
        }
        AddHandler boardPanel.Paint, AddressOf DrawBoardBorder
        Me.Controls.Add(boardPanel)

        ' ====== Create the 3x3 grid of buttons ======
        cells = New Button(2, 2) {}
        Dim btnSize As Integer = 110
        For row As Integer = 0 To 2
            For col As Integer = 0 To 2
                Dim btn As New Button() With {
                    .Name = $"cell_{row}_{col}",
                    .Location = New Point(col * btnSize + 2, row * btnSize + 2),
                    .Size = New Size(btnSize - 4, btnSize - 4),
                    .BackColor = Color.FromArgb(30, 40, 65),
                    .ForeColor = Color.White,
                    .Font = New Font("Segoe UI", 36.0F, FontStyle.Bold),
                    .FlatStyle = FlatStyle.Flat,
                    .Cursor = Cursors.Hand
                }
                AddHandler btn.MouseEnter, AddressOf CellMouseEnter
                AddHandler btn.MouseLeave, AddressOf CellMouseLeave
                AddHandler btn.Click, AddressOf Cell_Click
                cells(row, col) = btn
                boardPanel.Controls.Add(btn)
            Next
        Next

        ' ====== Reset button ======
        resetButton = New Button() With {
            .Text = "New Game",
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .Location = New Point(130, 500),
            .Size = New Size(160, 40),
            .BackColor = Color.FromArgb(0, 180, 255),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .TabStop = False
        }
        AddHandler resetButton.Paint, AddressOf DrawButtonGlow
        AddHandler resetButton.MouseEnter, AddressOf ResetMouseEnter
        AddHandler resetButton.MouseLeave, AddressOf ResetMouseLeave
        AddHandler resetButton.Click, AddressOf ResetButton_Click
        Me.Controls.Add(resetButton)
    End Sub

    Private Sub CellMouseEnter(sender As Object, e As EventArgs)
        Dim btn = DirectCast(sender, Button)
        btn.BackColor = Color.FromArgb(40, 55, 85)
    End Sub

    Private Sub CellMouseLeave(sender As Object, e As EventArgs)
        Dim btn = DirectCast(sender, Button)
        If board(BindingRowIndex(btn.Name), BindingColIndex(btn.Name)) <> 0 Then Return
        btn.BackColor = Color.FromArgb(30, 40, 65)
    End Sub

    Private Sub ResetMouseEnter(sender As Object, e As EventArgs)
        Dim btn = DirectCast(sender, Button)
        btn.BackColor = Color.FromArgb(30, 210, 255)
    End Sub

    Private Sub ResetMouseLeave(sender As Object, e As EventArgs)
        Dim btn = DirectCast(sender, Button)
        btn.BackColor = Color.FromArgb(0, 180, 255)
    End Sub

    Private Function BindingRowIndex(name As String) As Integer
        ' Extract row from name like "cell_1_2"
        Dim parts() As String = name.Split("_"c)
        Return CInt(parts(1))
    End Function

    Private Function BindingColIndex(name As String) As Integer
        Dim parts() As String = name.Split("_"c)
        Return CInt(parts(2))
    End Function

    Private Sub DrawBoardBorder(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        If boardPanel IsNot Nothing Then
            Using pen As New Pen(Color.FromArgb(0, 229, 255), 2.0F)
                g.DrawRectangle(pen, 1.0F, 1.0F, CSng(boardPanel.Width - 2), CSng(boardPanel.Height - 2))
            End Using

            ' Draw grid lines (separator between cells)
            Using pen As New Pen(Color.FromArgb(50, 70, 100), 1.5F)
                For i As Integer = 1 To 2
                    g.DrawLine(pen, CSng(i * 114 + 2), 2.0F, CSng(i * 114 + 2), CSng(boardPanel.Height - 2))
                    g.DrawLine(pen, 2.0F, CSng(i * 114 + 2), CSng(boardPanel.Width - 2), CSng(i * 114 + 2))
                Next
            End Using
        End If

        ' Rounded corners effect
        Using brush As New LinearGradientBrush(e.ClipRectangle,
                                                Color.FromArgb(8, 229, 255, 0),
                                                Color.FromArgb(15, 147, 60, 255),
                                                LinearGradientMode.ForwardDiagonal)
            g.FillRectangle(brush, e.ClipRectangle)
        End Using
    End Sub

    Private Sub DrawButtonGlow(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        If resetButton.Width <= 0 OrElse resetButton.Height <= 0 Then Return
        g.SmoothingMode = SmoothingMode.AntiAlias
        ' Gradient fill for button
        Using brush As New LinearGradientBrush(e.ClipRectangle,
                                                Color.FromArgb(0, 180, 255),
                                                Color.FromArgb(147, 60, 255),
                                                LinearGradientMode.Horizontal)
            g.FillRectangle(brush, e.ClipRectangle)
        End Using

        ' Rounded border
        Using pen As New Pen(Color.FromArgb(180, 200, 255), 1.0F)
            g.DrawRectangle(pen, 0.5F, 0.5F, CSng(resetButton.Width - 1), CSng(resetButton.Height - 1))
        End Using
    End Sub

    Private Sub Cell_Click(sender As Object, e As EventArgs)
        If Not isPlayerTurn OrElse gameOver Then Return

        Dim btn = DirectCast(sender, Button)
        Dim row = BindingRowIndex(btn.Name)
        Dim col = BindingColIndex(btn.Name)

        ' Cell already occupied
        If board(row, col) <> 0 Then Return

        ' Player places X
        board(row, col) = 1
        btn.Text = playerSymbol.ToString()
        btn.ForeColor = Color.FromArgb(0, 229, 255)
        btn.Refresh()

        ' Check for player win or tie
        Dim result = CheckWinner()
        If result <> 0 Then
            EndGame(result)
            Return
        End If

        ' Switch to computer turn
        isPlayerTurn = False
        statusLabel.Text = "AI is thinking..."
        statusLabel.ForeColor = Color.FromArgb(255, 180, 60)

        ' Delay slightly for UX feel, then computer moves
        Dim t As New Threading.Tasks.Task(Sub()
                                              Threading.Thread.Sleep(400)
                                              If Me.InvokeRequired Then
                                                  Me.Invoke(New Action(AddressOf MakeComputerMove))
                                              Else
                                                  MakeComputerMove()
                                              End If
                                          End Sub)
        t.Start()
    End Sub

    Private Sub MakeComputerMove()
        If gameOver Then Return

        ' Use minimax to find the best move
        Dim bestResult = FindBestMove()
        If bestResult IsNot Nothing Then
            board(bestResult.Value.R, bestResult.Value.C) = 2
            cells(bestResult.Value.R, bestResult.Value.C).Text = computerSymbol.ToString()
            cells(bestResult.Value.R, bestResult.Value.C).ForeColor = Color.FromArgb(255, 100, 100)
            cells(bestResult.Value.R, bestResult.Value.C).Refresh()
        End If

        ' Check for computer win or tie
        Dim result = CheckWinner()
        If result <> 0 Then
            EndGame(result)
            Return
        End If

        ' Back to player turn
        isPlayerTurn = True
        statusLabel.Text = "Your turn — click a cell"
        statusLabel.ForeColor = Color.FromArgb(200, 220, 255)
    End Sub

    Private Function FindBestMove() As BoardMove?
        Dim bestScore As Integer = Integer.MinValue
        Dim bestMove As BoardMove? = Nothing

        For r As Integer = 0 To 2
            For c As Integer = 0 To 2
                If board(r, c) <> 0 Then Continue For

                board(r, c) = 2 ' try computer move
                Dim score = Minimax(False, 0)
                board(r, c) = 0 ' undo

                If score > bestScore Then
                    bestScore = score
                    bestMove = New BoardMove(r, c)
                End If
            Next
        Next

        Return bestMove
    End Function

    Private Function Minimax(isMaximizing As Boolean, depth As Integer) As Integer
        Dim result = CheckWinner()

        If result = 2 Then Return ComputerScore - depth    ' computer wins (prefer faster win)
        If result = 1 Then Return PlayerScore + depth      ' player wins (delay loss)
        If result = 3 Then Return TieScore                 ' board full, it's a tie

        If isMaximizing Then
            Dim bestScore As Integer = Integer.MinValue
            For r As Integer = 0 To 2
                For c As Integer = 0 To 2
                    If board(r, c) <> 0 Then Continue For
                    board(r, c) = 2
                    Dim score = Minimax(False, depth + 1)
                    board(r, c) = 0
                    bestScore = Math.Max(bestScore, score)
                Next
            Next
            Return bestScore
        Else
            Dim bestScore As Integer = Integer.MaxValue
            For r As Integer = 0 To 2
                For c As Integer = 0 To 2
                    If board(r, c) <> 0 Then Continue For
                    board(r, c) = 1
                    Dim score = Minimax(True, depth + 1)
                    board(r, c) = 0
                    bestScore = Math.Min(bestScore, score)
                Next
            Next
            Return bestScore
        End If
    End Function

    Private Function CheckWinner() As Integer
        ' Check rows
        For r As Integer = 0 To 2
            If board(r, 0) <> 0 AndAlso board(r, 0) = board(r, 1) AndAlso board(r, 1) = board(r, 2) Then
                Return board(r, 0)
            End If
        Next

        ' Check columns
        For c As Integer = 0 To 2
            If board(0, c) <> 0 AndAlso board(0, c) = board(1, c) AndAlso board(1, c) = board(2, c) Then
                Return board(0, c)
            End If
        Next

        ' Check diagonals
        If board(0, 0) <> 0 AndAlso board(0, 0) = board(1, 1) AndAlso board(1, 1) = board(2, 2) Then
            Return board(0, 0)
        End If
        If board(0, 2) <> 0 AndAlso board(0, 2) = board(1, 1) AndAlso board(1, 1) = board(2, 0) Then
            Return board(0, 2)
        End If

        ' Check tie
        For r As Integer = 0 To 2
            For c As Integer = 0 To 2
                If board(r, c) = 0 Then Return 0
            Next
        Next

        Return 3 ' tie indicator
    End Function

    Private Sub EndGame(winner As Integer)
        gameOver = True

        If winner = 1 Then
            statusLabel.Text = "You win! Well played!"
            statusLabel.ForeColor = Color.FromArgb(60, 255, 140)
        ElseIf winner = 2 Then
            statusLabel.Text = "AI wins! Try again?"
            statusLabel.ForeColor = Color.FromArgb(255, 100, 100)
        Else
            statusLabel.Text = "It's a tie! Nice game."
            statusLabel.ForeColor = Color.FromArgb(200, 200, 200)
        End If

        ' Disable all cell buttons
        For r As Integer = 0 To 2
            For c As Integer = 0 To 2
                cells(r, c).Enabled = False
            Next
        Next
    End Sub

    Private Sub ResetGame()
        ' Clear the board
        board = New Integer(2, 2) {}
        gameOver = False
        isPlayerTurn = True

        ' Reset all cells
        For r As Integer = 0 To 2
            For c As Integer = 0 To 2
                cells(r, c).Text = String.Empty
                cells(r, c).ForeColor = Color.White
                cells(r, c).BackColor = Color.FromArgb(30, 40, 65)
                cells(r, c).Enabled = True
            Next
        Next

        statusLabel.Text = "Your turn — click a cell"
        statusLabel.ForeColor = Color.FromArgb(200, 220, 255)
    End Sub

    Private Sub ResetButton_Click(sender As Object, e As EventArgs)
        ResetGame()
    End Sub

End Class
