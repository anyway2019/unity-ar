# UnityAR
This project tend to show you how to implemation a unity ar demo by unity video player with tools like AE and PFtracker.

# Application
This project will not include it full feature implemation and just clarify how camera solver can apply into
unity3d.
I will show some full feature sample like the below:

```HTML
<video width:60% height:auto controls>
    <source src="https://www.youtube.com/watch?v=UR9HQHXQulk&t=558s" type="video/mp4">
</video>
```

# How it work?
- select a video and get image(jpg/png) sequence by tools like AE.
- import sequence into tool like PFtracker and export XMLCamera file after camera solver.
- parse XMLCamera file and convert it position and rotation information to left hand coordinate.
- render the real camera path with cube or virtual mesh trajectory over unity3d video player.
- more action can implemation with unity video player and it classic result in app like bkool and rouvy ar.

# How to get video sequence by AE
https://jingyan.baidu.com/article/ac6a9a5e5dea086a643eac30.html

# How to get XMLCamera PFtracker
you will get a simple tutorial there https://github.com/faaccy/unity-gizmos/issues/28.
if you still feel confused you can see some basic tutorial on youtube https://www.youtube.com/watch?v=5XOR0h0xKAo

# PFTracker Extensions
- Scene solver can export an video to 3d model https://www.youtube.com/watch?v=OwgBn4fRuoU
- Camera solver workflow with PFtrack AE and Cinema4d https://www.youtube.com/watch?v=E5hkRE70b7I
