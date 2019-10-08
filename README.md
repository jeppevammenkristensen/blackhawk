# Blackhawk
An api for generating code from Json, Csv and other formats that you can do repl like C# against

## Json example

```csharp
var output = await Build
                .Init()
                .WithConverter(new JsonLanguageConverter(new JsonConvertionSettings()
                {
                    UsePascalCase = true
                }))
                .GenerateSource(@"[
                    {
                        ""firstName"": ""Lars"",
                        ""lastName"": ""Ulrich""
                    },
                    {
                        ""firstName"": ""James"",
                        ""lastName"": ""Hetfield""
                    }
                ]")
                .Repl()
                .Execute("return input.OrderBy(x => x.FirstName);").ToJson();
            Console.WriteLine(output);
```


## Csv Example

```csharp
var output = await Build
                .Init()
                .WithConverter(new CsvLanguageConverter(new CsvConvertionSettings()
                {
                    Delimiter = ",",
                    FirstLineContainsHeaders = true
                }))
                .GenerateSource(@"FirstName,LastName
Lars,Ulrich
James,Hetfield")
                .Repl()
                .Execute("return input.OrderBy(x => x.FirstName);").ToJson();
            Console.WriteLine(output);
```
