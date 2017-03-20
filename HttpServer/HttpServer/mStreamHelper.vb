Imports System.Net
Imports System.Text

''' <summary>
''' Routines to support use of streams, particularly http.sys streams.
''' </summary>
Public Module mStreamHelper
	''' <summary>
	''' Resource not found (404) response
	''' </summary>
	''' <param name="context">Request conext</param>
	''' <returns></returns>
	Public Async Function ResourceNotFound(context As HttpListenerContext) As Task(Of Boolean)
		Return Await mStreamHelper.HttpErrorResponse(context, 404, "404 Resource not found")
	End Function

	''' <summary>
	''' Bad Request (400) response
	''' </summary>
	''' <param name="context"></param>
	''' <param name="message"></param>
	''' <returns></returns>
	Public Async Function BadRequest(context As HttpListenerContext, Optional message As String = "") As Task(Of Boolean)
		Dim desc As String = String.Empty
		If Not (String.Compare(context.Request.UserHostName, "localhost", True) = 0 OrElse String.Compare(context.Request.UserHostName, "127.0.0.1") = 0) Then
			message = "400 Bad request" ' Only show message on localhost
		Else
			desc = "400 " + message
		End If
		Return Await HttpErrorResponse(context, 400, desc)
	End Function

	''' <summary>
	''' Conflict (409) response.  
	''' </summary>
	''' <param name="context"></param>
	''' <returns></returns>
	Public Async Function Conflict(context As HttpListenerContext) As Task(Of Boolean)
		Return Await HttpErrorResponse(context, 409, "409 Conflict")
	End Function

	''' <summary>
	''' Unauthorized (401) response
	''' </summary>
	''' <param name="context"></param>
	''' <returns></returns>
	Public Async Function Unathorized(context As HttpListenerContext) As Task(Of Boolean)
		Return Await HttpErrorResponse(context, 401, "401 Unauthorized")
	End Function

	''' <summary>
	''' Method not allowed (405) response
	''' </summary>
	''' <param name="context"></param>
	''' <returns></returns>
	Public Async Function MethodNotAllowed(context As HttpListenerContext) As Task(Of Boolean)
		Return Await HttpErrorResponse(context, 405, "405 Method not allowed")
	End Function

	''' <summary>
	''' Internal server error (500) response
	''' </summary>
	''' <param name="context"></param>
	''' <returns></returns>
	Public Async Function InternalServerError(context As HttpListenerContext) As Task(Of Boolean)
		Return Await HttpErrorResponse(context, 500, "500 Internal server error")
	End Function

	''' <summary>
	''' Service Unavailable (503) response (Server is busy)
	''' </summary>
	''' <param name="context">Context of request</param>
	''' <returns></returns>
	Public Async Function ServiceUnavailable(context As HttpListenerContext) As Task(Of Boolean)
		Return Await HttpErrorResponse(context, 503, "503 Service unavailable")
	End Function
	''' <summary>
	''' OK (200) response for strings
	''' </summary>
	''' <param name="context">Request context</param>
	''' <param name="value">value to return in body</param>
	''' <returns></returns>
	Public Async Function OK(context As HttpListenerContext, value As String, Optional mimeType As String = "") As Task(Of Boolean)
		Return Await mStreamHelper.HttpResponse(context, 200, "OK", value, mimeType)
	End Function

	''' <summary>
	''' OK (200) response for byte arrays
	''' </summary>
	''' <param name="context">Request context</param>
	''' <param name="value">Byte() to return</param>
	''' <returns></returns>
	Public Async Function OK(context As HttpListenerContext, value As Byte(), Optional mimeType As String = "") As Task(Of Boolean)
		Return Await mStreamHelper.HttpResponse(context, 200, "OK", value, mimeType)
	End Function

	''' <summary>
	''' Write a string to stream
	''' </summary>
	''' <param name="stream">Stream to send string to</param>
	''' <param name="value">Value to write into stream</param>
	''' <returns></returns>
	Public Async Function WriteToStream(stream As IO.Stream, value As String) As Task(Of Boolean)
		Await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(value), 0, Len(value))
		Return True
	End Function

	''' <summary>
	''' Write a string followed by a Cr/Lf
	''' </summary>
	''' <param name="stream">Stream to write into</param>
	''' <param name="value">String to write into stream</param>
	''' <returns></returns>
	Public Async Function WriteLineToStream(stream As IO.Stream, value As String) As Task(Of Boolean)
		Await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(value), 0, Len(value))
		Await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(vbCrLf), 0, Len(vbCrLf))
		Return True
	End Function

	''' <summary>
	''' Read the body of a request as a string
	''' </summary>
	''' <param name="stream">Stream to read</param>
	''' <returns>String read.</returns>
	Public Async Function ReadStringFromStream(stream As IO.Stream) As Task(Of String)
		Dim buffer(65536) As Byte
		Dim sb As New StringBuilder
		Dim readLen = 65536
		While readLen > 0
			readLen = Await stream.ReadAsync(buffer, 0, readLen)
			If readLen > 0 Then
				sb.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, readLen))
			End If
		End While
		Return sb.ToString
	End Function

	''' <summary>
	''' Send generic HTTP response with string body
	''' </summary>
	''' <param name="context">Request context</param>
	''' <param name="status">HTTP status of response</param>
	''' <param name="statusDescription">Status description</param>
	''' <param name="value">String value for body</param>
	''' <returns></returns>
	Public Async Function HttpResponse(context As HttpListenerContext, status As Integer, statusDescription As String, value As String, Optional mimeType As String = "") As Task(Of Boolean)
		If context Is Nothing Then Return False
		If context.Response Is Nothing Then Return False
		Try
			If mimeType <> String.Empty Then
				context.Response.ContentType = mimeType
			End If
			context.Response.StatusCode = status
			context.Response.StatusDescription = statusDescription
			If value IsNot String.Empty Then
				context.Response.ContentLength64 = Len(value)
				Await mStreamHelper.WriteToStream(context.Response.OutputStream, value)
			End If
			context.Response.Close()
		Catch ex As Exception
			Stop
			Return False
		End Try
		Return True
	End Function

	''' <summary>
	''' Send generic HTTP response with byte() body
	''' </summary>
	''' <param name="context">Request context</param>
	''' <param name="status">HTTP status of response</param>
	''' <param name="statusDescription">Status description</param>
	''' <param name="value">Byte() to send</param>
	''' <returns></returns>
	Public Async Function HttpResponse(context As HttpListenerContext, status As Integer, statusDescription As String, value As Byte(), Optional mimeType As String = "") As Task(Of Boolean)
		If context Is Nothing Then Return False
		If context.Response Is Nothing Then Return False
		Try
			If mimeType <> String.Empty Then
				context.Response.ContentType = mimeType
			End If
			context.Response.StatusCode = status
			context.Response.StatusDescription = statusDescription
			If value IsNot Nothing Then
				context.Response.ContentLength64 = value.Length
				Await context.Response.OutputStream.WriteAsync(value, 0, value.Length)
			End If
			context.Response.Close()
		Catch ex As Exception
			Return False
		End Try
		Return True
	End Function

	Public Async Function HttpErrorResponse(context As HttpListenerContext, status As Integer, statusDescription As String) As Task(Of Boolean)
		context.Response.ContentType = "text/plain"
		Return Await HttpResponse(context, status, statusDescription, statusDescription)
	End Function
End Module