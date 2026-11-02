Public Class MySQLConnectionString
    Public Const connectionString As String = "server=localhost;user id=root;password=test;database=dbcpa"
End Class

Public Class countryaliases
    Property countryalias_id As Integer
    Property country_id As String
    Property [alias] As String
    Property alias_normalized As String
End Class
