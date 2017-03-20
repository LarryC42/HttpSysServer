Imports System.Net
Imports HttpServer

Public Class SimpleResponse
	Implements IHandleRequest
	Private _requests As Integer = 0

	Public Property Description As String Implements IHandleRequest.Description
		Get
			Return "Simple response"
		End Get
		Set(value As String)
		End Set
	End Property

	Public Async Function HandleAsync(context As HttpListenerContext, segments() As String, urlParameters As Dictionary(Of String, List(Of String))) As Task(Of Boolean) Implements IHandleRequest.HandleAsync
		Dim suffix As String = String.Empty
		_requests += 1
		Select Case _requests Mod 10
			Case 0, 4, 5, 6, 7, 8, 9 : suffix = "th"
			Case 1 : suffix = IIf(_requests = 11, "th", "st")
			Case 2 : suffix = IIf(_requests = 12, "th", "nd")
			Case 3 : suffix = IIf(_requests = 13, "th", "rd")
		End Select
		Return Await mStreamHelper.OK(context, String.Format("Hello for the {0}{1} time.", _requests, suffix), "text/plain")
		' or
		Return Await mStreamHelper.HttpResponse(context, 200, "OK", String.Format("Hello for the {0}{1} time.", _requests, suffix), "text/plain")
	End Function
End Class
