import cv2
import numpy as np
import os


def test():
    img = cv2.imread(r'C:\Projects\opencv\picture\test.jpg')
    template = cv2.imread(r'C:\Projects\opencv\picture\gold_2.png')
    w, h, c = template.shape
    res = cv2.matchTemplate(img, template, cv2.TM_CCOEFF_NORMED)
    threshold = 0.8
    loc = np.where(res >= threshold)
    for pt in zip(*loc[::-1]):
        cv2.rectangle(img, pt, (pt[0] + w, pt[1] + h), (0,0,255), 2)
    cv2.imwrite('res.png',img)


if __name__ == '__main__':
    print('main: hello, world')
    print(os.path.realpath(__file__))
    test()
