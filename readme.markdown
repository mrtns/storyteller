Why does `git status` show that all of my files are modified?
--
StoryTeller is built by Windows users, so all of the text files have CRLF line endings. These line endings are stored as-is in git (which means we all have autocrlf turned off).
If you have autocrlf enabled, when you retrieve files from git, it will modify all of your files. Your best bet is to turn off autocrlf, and refresh your working folder.

1. In the following steps, replace `global` with `system` to change the settings to be system wide. Alternatively, omit the option to affect only the current repo (your current directory must be in the StoryTeller repo.).
1. Type: `git config --global core.autocrlf false` to set up git behavior for your windows account.
1. Type: `git config --global core.whitespace cr-at-eol` to fix up the display of line endings in certain git commands such as `git diff`.
1. Type: `git reset --hard HEAD`
1. Type: `git clean -xdf`

Where is CommonAssemblyInfo.cs?
--

CommonAssemblyInfo.cs is generated by the build. The build script requires Ruby with rake installed.

1. open a command prompt to the root folder and type `rake` to execute rakefile.rb

If you do not have ruby:

1. You need to manually create a source\CommonAssemblyInfo.cs file 

  * type: `echo // > source\CommonAssemblyInfo.cs`
1. open source\StoryTeller.sln with Visual Studio and Build the solution
