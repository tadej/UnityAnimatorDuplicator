# UnityAnimatorDuplicator

Duplicates a Mechanim animator with the corresponding animations, saving them neatly in subfolders.
Supports layers and nested state machines.

![screenshot](https://github.com/tadej/UnityAnimatorDuplicator/raw/master/screens/screenshot.png "screenshot")

* right-click on an animator in Project view and select "Duplicate Animator and Animations"
* edit target animator name
* select target folder: existing or non-existing, can be nested e.g. /parentFolder/child folder/destination
* creates the folder path, duplicates the animator controller and animations and saves them in subfolders

# Sample Output
```
/parentFolder/child folder/destination/duplicateAnimator.controller
/parentFolder/child folder/destination/Animations/layer1/animation1.anim
/parentFolder/child folder/destination/Animations/layer1/animation2.anim
/parentFolder/child folder/destination/Animations/layer2/animation3.anim
```

# Compatibility

Tested with Unity 2018.2.18f1. Let me know if you find any issues with different versions.
