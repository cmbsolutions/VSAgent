<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        TabControlAdvanced1 = New TabControlAdvanced()
        TabPageAdvanced1 = New TabPageAdvanced()
        TabPageAdvanced2 = New TabPageAdvanced()
        TabControlAdvanced1.SuspendLayout()
        SuspendLayout()
        ' 
        ' TabControlAdvanced1
        ' 
        TabControlAdvanced1.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(36))
        TabControlAdvanced1.Controls.Add(TabPageAdvanced2)
        TabControlAdvanced1.Controls.Add(TabPageAdvanced1)
        TabControlAdvanced1.Font = New Font("Segoe UI", 9.5F)
        TabControlAdvanced1.Location = New Point(36, 76)
        TabControlAdvanced1.Name = "TabControlAdvanced1"
        TabControlAdvanced1.SelectedIndex = 0
        TabControlAdvanced1.Size = New Size(792, 200)
        TabControlAdvanced1.TabIndex = 0
        TabControlAdvanced1.Text = "TabControlAdvanced1"
        ' 
        ' TabPageAdvanced1
        ' 
        TabPageAdvanced1.BackColor = Color.White
        TabPageAdvanced1.Location = New Point(0, 36)
        TabPageAdvanced1.Name = "TabPageAdvanced1"
        TabPageAdvanced1.Size = New Size(792, 164)
        TabPageAdvanced1.TabIndex = 1
        TabPageAdvanced1.TabName = "TabPageAdvanced1"
        TabPageAdvanced1.TabStop = False
        TabPageAdvanced1.Text = "TabPageAdvanced1"
        ' 
        ' TabPageAdvanced2
        ' 
        TabPageAdvanced2.BackColor = Color.White
        TabPageAdvanced2.Location = New Point(0, 36)
        TabPageAdvanced2.Name = "TabPageAdvanced2"
        TabPageAdvanced2.Size = New Size(792, 164)
        TabPageAdvanced2.TabIndex = 1
        TabPageAdvanced2.TabName = "TabPageAdvanced2"
        TabPageAdvanced2.TabStop = False
        TabPageAdvanced2.Text = "TabPageAdvanced2"
        ' 
        ' Form1
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 450)
        Controls.Add(TabControlAdvanced1)
        Name = "Form1"
        Text = "Form1"
        TabControlAdvanced1.ResumeLayout(False)
        ResumeLayout(False)
    End Sub

    Friend WithEvents TabControlAdvanced1 As TabControlAdvanced
    Friend WithEvents TabPageAdvanced2 As TabPageAdvanced
    Friend WithEvents TabPageAdvanced1 As TabPageAdvanced

End Class
