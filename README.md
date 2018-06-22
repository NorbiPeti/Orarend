# Orarend
A school schedule app written in C# using Xamarin.

This app can obtain the schedule automatically from the school's website and update it as needed. Any temporary changes are marked red.

It downloads the site HTML and uses regex to fix some issues that the parser can't handle correctly and then parses the code.
