# Embel

A small, functional testing library similar to [Fuchu](https://github.com/mausch/Fuchu). I did not like how Fuchu uses discriminated unions for storing tests, so I rebuilt it from scratch so it uses functions with state passing instead. Ironically, this allows me to do things like actually share state which is necessary for some of my tests that I could not do with Fuchu.

As it is so small, the entirety of its functionality can be demostrated in 40LOC in the `Tests.fs` file.