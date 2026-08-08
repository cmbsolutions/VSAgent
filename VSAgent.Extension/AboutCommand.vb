Imports System.ComponentModel.Design
Imports System.Globalization
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Task = System.Threading.Tasks.Task

''' <summary>
''' Command handler
''' </summary>
Public NotInheritable Class AboutCommand

    ''' <summary>
    ''' Command ID.
    ''' </summary>
    Public Const CommandId As Integer = 256

    ''' <summary>
    ''' Command menu group (command set GUID).
    ''' </summary>
    Public Shared ReadOnly CommandSet As New Guid("96a426bf-2a1f-424a-a8aa-b68720050973")

    ''' <summary>
    ''' VS Package that provides this command, not null.
    ''' </summary>
    Private ReadOnly package As AsyncPackage

    ''' <summary>
    ''' Initializes a new instance of the <see cref="AboutCommand"/> class.
    ''' Adds our command handlers for menu (the commands must exist in the command table file)
    ''' </summary>
    ''' <param name="package">Owner package, not null.</param>
    Private Sub New(package As AsyncPackage, commandService As OleMenuCommandService)
        If package Is Nothing Then
            Throw New ArgumentNullException("package")
        End If

        If commandService Is Nothing Then
            Throw New ArgumentNullException(NameOf(commandService))
        End If

        Me.package = package

        Dim menuCommandId = New CommandID(CommandSet, CommandId)
        Dim menuCommand = New MenuCommand(AddressOf Me.Execute, menuCommandId)
        commandService.AddCommand(menuCommand)
    End Sub

    ''' <summary>
    ''' Gets the instance of the command.
    ''' </summary>
    Public Shared Property Instance As AboutCommand

    ''' <summary>
    ''' Get service provider from the owner package.
    ''' </summary>
    Private ReadOnly Property ServiceProvider As Microsoft.VisualStudio.Shell.IAsyncServiceProvider
        Get
            Return Me.package
        End Get
    End Property

    ''' <summary>
    ''' Initializes the singleton instance of the command.
    ''' </summary>
    ''' <param name="package">Owner package, Not null.</param>
    Public Shared Async Function InitializeAsync(package As AsyncPackage) As Task
        ' Switch to the main thread - the call to AddCommand in AboutCommand's constructor requires
        ' the UI thread.
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken)

        Dim commandService As OleMenuCommandService = Await package.GetServiceAsync(GetType(IMenuCommandService))
        Instance = New AboutCommand(package, commandService)
    End Function

    ''' <summary>
    ''' This function is the callback used to execute the command when the menu item is clicked.
    ''' See the constructor to see how the menu item is associated with this function using
    ''' OleMenuCommandService service and MenuCommand class.
    ''' </summary>
    ''' <param name="sender">Event sender.</param>
    ''' <param name="e">Event args.</param>
    Private Sub Execute(sender As Object, e As EventArgs)
        ThreadHelper.ThrowIfNotOnUIThread()

        Dim message = String.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", Me.GetType().FullName)
        Dim title = "AboutCommand"

        ' Show a message box to prove we were here
        VsShellUtilities.ShowMessageBox(
            Me.package,
            message,
            title,
            OLEMSGICON.OLEMSGICON_INFO,
            OLEMSGBUTTON.OLEMSGBUTTON_OK,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST)
    End Sub
End Class
