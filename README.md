# HexMap
地图

[Unity 六边形地图系列(一) : 创建一个六边形网格](http://gad.qq.com/program/translateview/7173811)

> 难点

1. X轴的偏移导致的算法的不通 
2. Y值的产生

[Unity 六边形地图系列(二) : 混合单元颜色](http://gad.qq.com/program/translateview/7173943)

> 难点

1. 三角化混合区域时triangles的添加为021123
2. 桥化时（取两个相关角的中点，然后对它使用混合因子来得到偏移量）
3. 填充空隙时候的两个三角形