# RunScripts 

Declarative database updates.

Handling database changes through migration scripts doesn't work well with Git. A single view could be spread across multiple migration scripts making tracing a change a very manual process.

RunScripts allows you to do things differently, by scripting each database object as a separate .sql file change tracking is able to work in the same way as your application code. When a database object is created it shows up as a new file in Git, then when it is changed it is just a file change.

Running `rs -run` will apply the any changed objects and only changed objects to the database. When working with other developers adding `rs -run` as a post build event will sync you local database to the current branch before starting the app. Or adding `rs -run` to an update script will update the staging or live database to match the code version getting pushed.
