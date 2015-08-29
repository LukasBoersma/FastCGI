del FastCGI.*.nupkg
nuget pack -Prop Configuration=Release
nuget push FastCGI.*.nupkg