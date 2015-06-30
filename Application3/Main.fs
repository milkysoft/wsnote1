namespace WebBlogApplication

open WebSharper.Html.Server
open WebSharper
open WebSharper.Sitelets



type Action =
    | Home
    | Login of Action
    | CreateNewPost
     
module Skin =
    open System.Web

    type Page =
        {   Title : string
            Content : Element list }

    let mainTemplate =
        Content.Template<Page>("~/Main.html")
            .With("title", fun x -> x.Title)
            .With("content", fun x -> x.Content)

    let withTemplate title content =
        Content.WithTemplate mainTemplate <| fun context ->
            {   Title = title
                Content = content context }

    let withTemplateAsync title content : Content<Action>  =
        Content.WithTemplateAsync mainTemplate <| fun context ->
            async{
                let! content = content context
                return 
                    {   Title = title
                        Content = content }  }

module Site =
    let HomePage = Skin.withTemplateAsync "HomePage" <| fun context -> async {
        
        //let! blog = DataBaseAgents.getPage 0 20 None
        
        let! user = context.UserSession.GetLoggedInUser()
        let is'admin = user.IsSome
        

        return 
            [   if user.IsNone then
                    yield P [ A [ Home |> Login |> context.Link |> Attr.HRef  ] -< [ Text "Войти на сайт" ] ] 
                yield P [ClientSide <@ BlogUI.Blog(is'admin) @>] ] }

    
    let LoginPage redirectAction = 
        Skin.withTemplate "Авторизация" <| fun ctx ->     
            let url = ctx.Link redirectAction
            [   H1 [Text "Авторизация"]
                P [
                    Text "Войти на сайт под любым именем пользователя с паролем "
                    I [Text "'password'"]
                    Text "'." ]                
                Div [ ClientSide <@ LoginUI.Login url @> ] ] 

       
    let MainSitelet =
        Sitelet.InferPartial id 
            (function 
                | Login _ | CreateNewPost  -> None
                | x -> Some x)
            (function
                | Home -> HomePage
                | _ -> Content.NotFound ) <|> 
        Sitelet.InferPartial id 
            (function 
                | Login x -> Login x |> Some
                | _ -> None)
            (function
                | Login x -> LoginPage x
                | _ -> Content.NotFound )
    let Main =
        Sitelet.Sum [
            Sitelet.Content "/" Home HomePage
            //Sitelet.Content "/Login" Login LoginPage
        ]

        
[<Sealed>]
type Website() =
    interface IWebsite<Action> with
        member this.Sitelet =  Site.Main
        member this.Actions = [Home]

type Global() =
    inherit System.Web.HttpApplication()

    member g.Application_Start(sender: obj, args: System.EventArgs) =
        ()

[<assembly: Website(typeof<Website>)>]
do ()
