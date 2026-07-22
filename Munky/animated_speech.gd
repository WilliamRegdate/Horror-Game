extends Label

var speech = "me fat munky.\nme like banana.\nif find banana give munky banana.\nMunky very hungry.\nif find munky 5 banana munky give reward."
@export var audio_player : AudioStreamPlayer2D
@export var sounds: Array[AudioStream]



@export var shake_amount :float = 1.0 ##amount the lettering will shake. 
@export var speed : float = 1.0  ##speed at which the animation will play
@export var talk_amount : int = 2 ## how many letters it takes for speech sound to play

var _playback: AudioStreamPlaybackPolyphonic

var timer : float = 0
var index : int = 0
var has_text_finished : bool = true

func _ready() -> void:
	self.offset_transform_pivot = self.size /2
	var poly_stream = AudioStreamPolyphonic.new()
	poly_stream.polyphony = 8
	audio_player.stream = poly_stream
	audio_player.play()
	_playback = audio_player.get_stream_playback()

func play_sound() -> void:
	var sound = sounds[randi() % sounds.size()]
	_playback.play_stream(sound)
	

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey:
		if event.pressed and event.keycode == KEY_ESCAPE:
			speak_text(speech)

func speak_text(input_text):
	has_text_finished = false
	index = 0
	timer = 0.5
	speech = input_text
	self.text = ""


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	#text has finished adding dont add any more text
	if has_text_finished == true:
		offset_transform_scale = offset_transform_scale.move_toward(Vector2(0,0),delta *2)
		return
	else:
		offset_transform_scale = offset_transform_scale.move_toward(Vector2(1,1),delta *2)

	if timer > 0:
		timer -= delta
		return
	else:
		timer = 0.05 / speed
		if index >= speech.length():

			self.offset_transform_rotation = 0
			self.offset_transform_position = Vector2(0,0)
			has_text_finished = true
			return
		self.text += speech[index]
		self.offset_transform_position = Vector2((randf() * 6 * shake_amount) -3 * shake_amount,(randf() * 6 * shake_amount) -3 * shake_amount)
		self.offset_transform_rotation = 0
		if speech[index] != ' ' and index % talk_amount == 0:
			play_sound()
		if speech[index] == '.':
			timer += 0.7 / speed
		index +=1
