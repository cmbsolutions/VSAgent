<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SnakeGameForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.gamePanel = New System.Windows.Forms.Panel()
        Me.scoreLabel = New System.Windows.Forms.Label()
        Me.statusText = New System.Windows.Forms.Label()
        Me.restartButton = New System.Windows.Forms.Button()
        Me.gameTimer = New System.Windows.Forms.Timer(Me.components)
        
        'gamePanel
        Me.gamePanel.Location = New System.Drawing.Point(10, 40)
        Me.gamePanel.Name = "gamePanel"
        Me.gamePanel.Size = New System.Drawing.Size(600, 400)
        Me.gamePanel.BorderStyle = BorderStyle.FixedSingle
        Me.gamePanel.BackColor = Color.Black
        
        'scoreLabel
        Me.scoreLabel.AutoSize = True
        Me.scoreLabel.Font = New System.Drawing.Font("Segoe UI", 12.0F, FontStyle.Bold)
        Me.scoreLabel.ForeColor = Color.White
        Me.scoreLabel.Location = New System.Drawing.Point(10, 9)
        Me.scoreLabel.Name = "scoreLabel"
        Me.scoreLabel.Size = New System.Drawing.Size(85, 21)
        Me.scoreLabel.Text = "Score: 0"
        
        'statusText
        Me.statusText.AutoSize = True
        Me.statusText.Font = New System.Drawing.Font("Segoe UI", 10.0F, FontStyle.Italic)
        Me.statusText.ForeColor = Color.LightGray
        Me.statusText.Location = New System.Drawing.Point(400, 9)
        Me.statusText.Name = "statusText"
        Me.statusText.Size = New System.Drawing.Size(210, 19)
        Me.statusText.Text = "Use Arrow Keys to Control the Snake"
        
        'restartButton
        Me.restartButton.Location = New System.Drawing.Point(500, 446)
        Me.restartButton.Name = "restartButton"
        Me.restartButton.Size = New System.Drawing.Size(90, 30)
        Me.restartButton.TabIndex = 1
        Me.restartButton.Text = "Restart"
        Me.restartButton.UseVisualStyleBackColor = True
        Me.restartButton.Visible = False
        
        'gameTimer
        Me.gameTimer.Interval = 120

        'SnakeGameForm
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0F, 13.0F)
        Me.AutoScaleMode = AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(620, 485)
        Me.Controls.Add(Me.restartButton)
        Me.Controls.Add(Me.statusText)
        Me.Controls.Add(Me.scoreLabel)
        Me.Controls.Add(Me.gamePanel)
        Me.Name = "SnakeGameForm"
        Me.Text = "Snake Game"
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.KeyPreview = True
        
    End Sub

    Friend WithEvents gamePanel As Panel
    Friend WithEvents scoreLabel As Label
    Friend WithEvents statusText As Label
    Friend WithEvents restartButton As Button
    Friend WithEvents gameTimer As Timer

End Class