''' <summary>
''' General URL helper routines
''' </summary>
Module mUrlHelper
	''' <summary>
	''' Get parameters (values after the ? in the URL)
	''' </summary>
	''' <param name="url">URL to get parameters from</param>
	''' <returns>Dictionary(Of List(Of String)): Each name will exist once with each of the values for the parameter</returns>
	Function GetUrlParameters(ByVal url As String) As Dictionary(Of String, List(Of String))
		Dim params As New Dictionary(Of String, List(Of String))(StringComparer.CurrentCultureIgnoreCase)
		Dim p = InStr(url, "?")
		If p = 0 Then Return params
		url = Mid(url, p + 1)
		Dim pairs() = Split(url, "&")
		For Each pair In pairs
			Dim parts() = Split(pair, "=")
			Dim key = LCase(parts(0))
			Dim value = If(parts.Length < 2, String.Empty, parts(1))
			If Not params.ContainsKey(key) Then
				params.Add(key, New List(Of String))
			End If
			params(key).Add(value)
		Next
		Return params
	End Function

	''' <summary>
	''' Get the URL segments after the root path up to ? if any in the URL.
	''' </summary>
	''' <param name="url">URL to get segments from</param>
	''' <param name="rootPath">Root path of server</param>
	''' <returns>String(): with a value for each segment</returns>
	Public Function GetUrlSegments(ByVal url As String, rootPath As String) As String()
		Dim p = InStr(url, "?")
		If p > 0 Then url = Left(url, p - 1)
		If url.Length > 0 AndAlso url(0) = "/" Then url = Mid(url, 2)
		If rootPath.Length > 0 Then
			If String.Compare(Left(url, rootPath.Length), rootPath, True) = 0 Then
				url = Mid(url, rootPath.Length + 1)
			End If
		End If
		Return Split(url, "/")
	End Function

	''' <summary>
	''' Get the non-parameter segments of the URL
	''' </summary>
	''' <param name="url">URL to get segments from</param>
	''' <param name="rootPath">Root path of service</param>
	''' <returns></returns>
	Public Function GetCommandSegments(ByVal url As String, rootPath As String) As String()
		Dim list As New List(Of String)
		For Each segment In GetUrlSegments(url, rootPath)
			If segment(0) <> "{"c Then list.Add(segment)
		Next
		Return list.ToArray
	End Function
End Module