<h1 align="center">
  <br>
  <a href="https://github.com/rainerzufalldererste/LamestWebserver"><img src="https://raw.githubusercontent.com/rainerzufalldererste/LamestWebserver/master/LamestWebserver/content/lws-promo.png" alt="LamestWebserver" style="width: 700px; max-width: 80%"></a>
  <br>
</h1>

LamestWebserver is an open-source WebApp- and Webserver framework built to be simple and easy to use but still powerful and low-level.
It's mainly written in C# and has very few dependencies to be easily portable.

## Quickstart Guide

Starting a Webserver is easy:

``` c#
using LamestWebserver;

// Create a new Webserver on port 80 that tries to read static files (like images or stylesheets)
//  from the subfolder "./web" based on the execution directory.
using (new WebServer(80, "./web"))
{
    // Automatically discovers all dynamic response-pages in this assembly and registers them at the webserver.
    Master.DiscoverPages();
    
	Console.WriteLine("LamestWebserver Demo.\nEnter 'exit' to quit.");
	
    // Keep the Server available until we enter exit.
    while (Console.ReadLine() != "exit") { }
}
```


The dynamic pages to respond by the webserver just have to inherit from one of the response types like `PageResponse` or `ElementResponse`.

``` c#
using LamestWebserver;
using LamestWebserver.UI;

// The class of a page has to inherit i.e. from `ElementResponse` to be automatically discoverable.
public class Page : ElementResponse
{
    // The constructor of this page sets the URL that the page shall be available at by calling `base(<URL>)`.
    // It also has to be empty (like this one) to be able to be automatically called.
    public Page() : base("/") { }
    
    // This method will be called whenever the page is requested.
    protected override HElement GetElement(SessionData sessionData)
    {
        // LamestWebserver.UI contains a lot of HTML-Elements that can be constructed and returned like this.
        return new HHeadline("LamestWebserver Test Page") + new HText("Hello World.");
    }
}
```


The project also includes a `Demo` sub-project, that includes further information and examples.

## Motivation

LamestWebserver is an answer to all the bloated and over-complicated WebApp solutions currently available.
It wants to be small, fast & easy to use.

## Contributors

Contributions are very welcome. 
Please read our [`coding-style.md`](https://github.com/rainerzufalldererste/LamestWebserver/blob/master/coding-style.md) and check our `Projects` and `Issues` sections of our github page to find issues to help with. 
If you have any questions regarding this project also feel free to get in touch or comment on it.

## License

[LGPL](https://github.com/rainerzufalldererste/LamestWebserver/blob/master/LICENSE)
