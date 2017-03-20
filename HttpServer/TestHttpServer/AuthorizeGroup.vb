Imports System.Net
Imports HttpServer

''' <summary>
''' To test, create a local group (or domain group) called test.  If you're in the group this will authorize
''' otherwise you'll get a 401.
''' </summary>
Public Class AuthorizeGroup
	Implements IHandleRequest
	Public Property Group As String = "Test"
	Public Property Description As String Implements IHandleRequest.Description
		Get
			Return "Authorize group"
		End Get
		Set(value As String)
		End Set
	End Property

	Public Async Function HandleAsync(context As HttpListenerContext, segments() As String, urlParameters As Dictionary(Of String, List(Of String))) As Task(Of Boolean) Implements IHandleRequest.HandleAsync
		Await Task.Delay(0)
		If Not context.Request.IsAuthenticated Then Return False
		If context.User IsNot Nothing Then
			Return context.User.IsInRole(Group)
		End If
		Return False
	End Function
End Class
