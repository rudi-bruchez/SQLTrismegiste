## Sql Trismegiste ##

**Sql Trismegiste** is a free and open source SQL Server diagnostic tool.

It can run diagnostic queries against a SQL Server, from SQL Server 2005 onwards. The diagnostic queries are stored in xml documents and your can create your own queries and add them in Sql Trismegiste, by creating new xml definitions. The syntax is simple.

### Dependencies ###

Sql Trismegiste is written in .NET (C#, WPF). You need to have the **.NET 4.5 framework** installed for it to work (client profile is fine).

### How it works ###

The results are stored in the SqlTrismegiste folder inside your Documents. There is a subdirectory per server your connected to. Queires results are stored in HTML files and can be opened manually. There are log files per day that store potential errors and warnings about Sql Trismegiste processing.

### Hermetica ###

The diagnostic queries are stored in XML files, inside the `CorpusHermeticum` folder. To add your own query, simply copy an existing xml file and edit it. You need to specify :

- The `name` of the hermeticus.
- The `level` : it can be `Server` or `Database`. A Database-level query will be run against all selected databases of your server and produce one result per database. When you write a Database level query, assume it will be run in the context of each database.
- the `folder`. It is the name of the folder in which the result will appear in the Sql Trismegiste tree view. The folder name must exist in the `Folders.xml` file, inside the `CorpusHermeticum` folder. You can add new folders in this file.

Then, you can write multiple versions of your query, for different versions (MajorVersion) of SQL Server. Sql Trismegiste will choose the query accordingly. It will execute the query defined for the version of SQL server it is connected to, or the closest previous version, or the query marked as `*` for any version. You can write only one query and mark it as `*` if your query is not version-specific.

### Functionalities ###

- You can zip the results folder and send it by email.

### Not yet implemented ###

- automatic expanding of tree view after processing.
- `Save Sql Text` and `Save Query Plans` is almost done. It can save query plans and sql texts when you check it and Sql Trismegiste sees `plan_handle` or `sql_handle` columns.

Enjoy first-class Markdown support with easy access to  Markdown syntax and convenient keyboard shortcuts.

Give them a try:

- **Bold** (`Ctrl+B`) and *Italic* (`Ctrl+I`)
- Quotes (`Ctrl+Q`)
- Code blocks (`Ctrl+K`)
- Headings 1, 2, 3 (`Ctrl+1`, `Ctrl+2`, `Ctrl+3`)
- Lists (`Ctrl+U` and `Ctrl+Shift+O`)

### See your changes instantly with LivePreview ###

Don't guess if your [hyperlink syntax](http://markdownpad.com) is correct; LivePreview will show you exactly what your document looks like every time you press a key.

### Make it your own ###

Fonts, color schemes, layouts and stylesheets are all 100% customizable so you can turn MarkdownPad into your perfect editor.

### A robust editor for advanced Markdown users ###

MarkdownPad supports multiple Markdown processing engines, including standard Markdown, Markdown Extra (with Table support) and GitHub Flavored Markdown.

With a tabbed document interface, PDF export, a built-in image uploader, session management, spell check, auto-save, syntax highlighting and a built-in CSS management interface, there's no limit to what you can do with MarkdownPad.
