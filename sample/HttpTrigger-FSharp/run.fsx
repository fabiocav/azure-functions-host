open System
open System.Linq
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Primitives

let Run (req: HttpRequest, log: TraceWriter) : IActionResult =
    log.Verbose(sprintf "F# HTTP trigger function processed a request.")

    let res : IActionResult =
        match req.Query.TryGetValue("name") with 
        | true, name -> 
            upcast new OkObjectResult("Hello " + name.ToString())
        | _ -> 
            upcast new BadRequestObjectResult("Please pass a name on the query string")
    
    res
