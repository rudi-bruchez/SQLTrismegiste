## Sql Trismegiste ##

**Sql Trismegiste** est un outil libre et open source (license Apache 2) de diagnostic pour SQL Server.

A l'heure actuelle, Sql Trismegiste est en version alpha. Le programme est fonctionnel, et beaucoup d'erreurs possibles sont gérées et logguées, mais je peux avoir oublié des cas possibles. Merci de me communiquer tout cas de crash.

Il peut exécuter des requêtes de diagnostic (typiquement des interrogations de vues de gestion dynamique) sur un serveur SQL server, à partir de la version 2005.

Il y a à l'heure actuelle près de 60 requêtes de diagnostic sur tous les aspects de SQL Server.

Les requêtes de diagnostic sont modifiables. Chaque requête est exprimée dans un fichier de définition en XML et vous pouvez aussi bien modifier les requêtes existantes que créer les vôtres. La syntaxe est simple.

Vous êtes encouragés à partager vos requêtes. Envoyez-moi vos requêtes sous forme d'hermetica (voir ci-dessous) et je les ajouterai à ce dépôt.

### POUR TELECHARGER ###
- Récupérez le binaire dans le répertoire https://github.com/rudi-bruchez/SQLTrismegiste/tree/master/distributions.
- Cliquez sur le .zip de la dernière version.
- Cliquez sur "View Raw" pour télécharger le fichier.

### Dépendances ###

Sql Trismegiste est écrit en .NET (C#, WPF). Si ce n'est pas déjà fait, vous devez installer le **Framework .NET 4.5** pour le faire fonctionner (la version redistribuable *client profile* suffit).

### Comment ça marche ###

Sql Trismegiste de connecte à un serveur et exécute toutes les requêtes de diagnostic définies. Il récupère les résultats dans un format de table HTML qu'il stocke dans un répertoire de votre disque. Vous pouvez ensuite les visualiser directement dans Sql Trismegiste, ou ouvrir manuellement les fichiers HTML dans un navigateur. Vous pouvez créer automatiquement un fichier zip avec ce résultat, et l'envoyer en pièce jointe par e-mail. Cela vous permet de communiquer des informations de performance intéressantes sur votre serveur pour analyse.

Sql Trismegiste enregistre également, dans le même répertoire, un fichier log pour indiquer les éventuelles erreurs d'exécutions (requêtes incorrectes par exemple).

### Hermetica ###

Les requêtes de diagnostic sont stockées dans des fichiers XML nommés des Hermetica, dans le répertoire de l'application `CorpusHermeticum`. Pour ajoutez vos propres requêtes, copiez simplement un fichier existant avec un nouveau nom et changez le contenu. Vous devez modifier :

- Le nom (`name`) de l'hermeticus.
- Le niveau (`level`). Les valeurs possibles sont `Server` ou `Database`. Une requête de niveau serveur s'exécute dans le contexte de la base `master`. Une requête de niveau Database s'exécute autant de fois que vous avez coché de base de données dans Sql Trismegiste, dans le contexte de chaque base de données. Le résultat sera donc une table par base de données. Pour écrire une requête de niveau Database, vous partez donc du principe qu'elle s'exécute chaque fois dans un contexte de base de données utilisateur.
- Le dossier (`folder`). Il s'agit du nom du dossier d'affichage dans Sql Trismegiste (vue hiérarchique des requêtes à gauche. Cette vue est dynamique et vous pouvez créer vos propres dossiers en créant une nouvelle entrée dans le fichier `Folders.xml` dans le répertoire `CorpusHermeticum`.

Vous pouvez écrire plusieurs versions de votre requête dans le même hermeticus, pour l'adapter aux différentes versions de SQL Server. Pour chaque requête, vous devez indiquer l'attribut `MajorVersion`, qui correspond à la même valeur que le `MajorVersion` dans SQL Server, bien sûr. Vous pouvez trouver une liste des versions de SQL Server ici : [http://sqlserverbuilds.blogspot.fr/](http://sqlserverbuilds.blogspot.fr/ "sqlserverbuilds"). Sql Trismegiste choisira la bonne requête à l'exécution selon votre connexion. 
La règle est la suivante :
1. Vous pouvez indiquer un simple nombre (11 par exemple), cette requête ne sera utilisée que pour la version en question.
1. Vous pouvez indiquer une étoile (*). Cette requête sera utilisée pour toutes les versions.
1. vous pouvez indiquer une plage en indiquant une version de début et une version de fin, inclusives, séparées par un tiret (signe moins : -), avec la possibilité de mettre une étoile. Par exemple "11-*", "*-11", "11-12". "11-*" signifie "de 11 à toutes les versions suivantes.

Vous pouvez valider votre hermeticus à l'aide du schéma XML `Corpus.xsd` dans le répertoire `CorpusHermeticum`.

### Fonctionnalités en cours ###

- Les options `Ajouter requêtes` et `Ajouter plan` sont implémentées à 50 %. Le but est de sauvegarder le texte SQL des requêtes et/ou le plan d'exécution en XML dès qu'une requête retourne une colonne nommée `plan_handle` ou `sql_handle`. Cela sera ajouté dans dans fichiers distincts avec un lien pour les ouvrir dans le résultat HTML.
- La localisation en anglais est en cours.
