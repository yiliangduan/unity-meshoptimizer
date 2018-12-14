### Texture合并成Atlas工具

在Unity的Project窗口中，选中一个存放图片的文件夹。右键菜单中可以看到 [Pack Texture], [Pack Texture(All Relayout)] 选项, 即可合并该文件夹下的所有贴图.

合并贴图之后会生成对应的Asset文件和Atlas文件。Asset文件保存了Atlas文件中每张贴图在图集中的偏移，缩放和对原始图片的引用。

注: 
* Pack Texture:  如果之前合并过贴图，该种模式下会继承上次贴图的合并后在图集中的偏移，不会改动之前已经合过贴图的位置。
* Pack Texture(All Relayout): 全部重新排列合图，这种情况下可能改变之前已经合过贴图的在图集中的偏移。