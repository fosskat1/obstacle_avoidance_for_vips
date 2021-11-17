import numpy as np
import matplotlib.pyplot as plt
import os
import cv2

from PIL import Image
from PIL import ImageColor
from PIL import ImageDraw
from PIL import ImageFont
from PIL import ImageOps

MAIN_DIR = './Assets/ImageOutputs'


def draw_bounding_box_on_image(
		image,
		ymin,
		xmin,
		ymax,
		xmax,
		color,
		font,
		thickness=4,
		display_str_list=()
    ):
	"""Adds a bounding box to an image."""
	draw = ImageDraw.Draw(image)
	im_width, im_height = image.size
	(left, right, top, bottom) = (
		xmin, #  * im_width, 
		xmax, #  * im_width,
		ymin, #  * im_height, 
		ymax, #  * im_height
		)
	draw.line([
		(left, top), (left, bottom), (right, bottom), (right, top), (left, top)
		],
		width=thickness,
		fill=color
		)

	# If the total height of the display strings added to the top of the bounding
	# box exceeds the top of the image, stack the strings below the bounding box
	# instead of above.
	display_str_heights = [font.getsize(ds)[1] for ds in display_str_list]
	# Each display_str has a top and bottom margin of 0.05x.
	total_display_str_height = (1 + 2 * 0.05) * sum(display_str_heights)

	if top > total_display_str_height:
		text_bottom = top
	else:
		text_bottom = top + total_display_str_height
	# Reverse list and print from bottom to top.
	for display_str in display_str_list[::-1]:
		text_width, text_height = font.getsize(display_str)
		margin = np.ceil(0.05 * text_height)
		draw.rectangle([(left, text_bottom - text_height - 2 * margin),
		                (left + text_width, text_bottom)],
		               fill=color)
		draw.text((left + margin, text_bottom - text_height - margin),
		          display_str,
		          fill="black",
		          font=font)
		text_bottom -= text_height - 2 * margin

if __name__ == '__main__':
	file_list = os.listdir(MAIN_DIR)

	jpg_files = sorted([x for x in file_list if x.endswith('.jpg')])
	txt_files = sorted([x for x in file_list if x.endswith('.txt')])

	for file_idx in range(len(jpg_files)):
		jpg_file = str(file_idx) + '.jpg'
		txt_file = str(file_idx) + '.txt'

		try:
			with open(os.path.join(MAIN_DIR, txt_file), 'r') as f:
				data = [x.split(',') for x in f.readlines()]
				# print(data)
				boxes = [d[:4] for d in data]
				# print(boxes)
				boxes = [[round(float(v)) for v in d] for d in boxes]
				label_text = [d[4:] for d in data]
				label_text = [[x[0], str(round(float(x[1])*100))] for x in label_text]

				# boxes = [[round(float(v)) for v in x.split(',')[:4]] for x in f.readlines()]
		except FileNotFoundError:
			print('Error in file ', txt_file)
			continue

		# print(boxes)
		# image = cv2.imread(os.path.join(MAIN_DIR, jpg_file))
		# image = cv2.cvtColor(image)
		image = Image.open(os.path.join(MAIN_DIR, jpg_file))
		color = list(ImageColor.colormap.values())
		font = ImageFont.load_default()

		for idx, b in enumerate(boxes):
			x1 = b[0]
			y1 = b[1]
			x2 = x1 + b[2]
			y2 = y1 + b[3]

			# cv2.rectangle(image, (x1, y1), (x2, y2), 255, 2)
			
			draw_bounding_box_on_image(
				image,
				y1,
				x1,
				y2,
				x2,
				color[0],
				font,
				display_str_list=label_text[idx]
		    )


		# cv2.imwrite('BoxOutputs/' + str(file_idx) + '_box.jpg', image)

		image.save('BoxOutputs/' + str(file_idx) + '_box.jpg')