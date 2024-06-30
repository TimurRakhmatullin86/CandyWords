namespace Managers {
    using Structures;
    using UI;
    using UnityEngine;

    public class AudioManager : MonoBehaviour {
        public static AudioManager Instance;

        [SerializeField] private AudioSource  musicAudioSource;
        [SerializeField] private AudioSource  soundsAudioSource;
        [SerializeField] private ControlPanel controlPanel;

        [SerializeField] private AudioClip[] musicSounds;
        [SerializeField] private AudioClip[] candySounds;
        
        [SerializeField] private AudioClip swipedSound;

        [SerializeField] private AudioClip stripedSound;
        [SerializeField] private AudioClip bombSound;
        [SerializeField] private AudioClip multipleBombSound;

        [SerializeField] private AudioClip levelComplete;
        [SerializeField] private AudioClip levelLoosed;

        [SerializeField] private AudioClip allAboard;

        [SerializeField] private AudioClip wrappedBombCreated;
        [SerializeField] private AudioClip multipleBombCreated;
        [SerializeField] private AudioClip stripedCandyCreated;

        private bool isOnMute;

        private void Awake() {
            Instance = this;

            this.StartPlaying();

            this.controlPanel.MusicControl.onClick.AddListener(() => {
                this.isOnMute = !this.isOnMute;

                if (this.isOnMute) {
                    this.musicAudioSource.Pause();
                    this.controlPanel.MusicControlText.text = "Play Music";
                }
                else {
                    this.musicAudioSource.UnPause();
                    this.controlPanel.MusicControlText.text = "Stop Music";
                }
            });
        }

        public void StartPlaying() {
            this.musicAudioSource.Stop();
            this.soundsAudioSource.clip = this.allAboard;
            this.soundsAudioSource.PlayDelayed(0.3f);
            this.musicAudioSource.clip = this.musicSounds[Random.Range(0, this.musicSounds.Length)];
            this.musicAudioSource.PlayDelayed(this.allAboard.length);
            
            this.isOnMute = false;
            this.controlPanel.MusicControlText.text = "Stop Music";
        }

        public void StopPlayingMusic() {
            this.musicAudioSource.Stop();
            this.isOnMute = true;
            this.controlPanel.MusicControlText.text = "Play Music";
        }

        public void OnCandyDestroyed((CandyColor color, CandyType type) valueTuple) {
            switch (valueTuple.type) {
                case CandyType.Simple:
                    this.soundsAudioSource.PlayOneShot(this.candySounds[Random.Range(0, this.candySounds.Length)]);

                    break;

                case CandyType.Bomb:
                    if (valueTuple.color == CandyColor.Multiple) {
                        this.soundsAudioSource.PlayOneShot(this.multipleBombSound);
                    }
                    else {
                        this.soundsAudioSource.PlayOneShot(this.bombSound);
                    }

                    break;

                case CandyType.StripedHor:
                    this.soundsAudioSource.PlayOneShot(this.stripedSound);

                    break;

                case CandyType.StripedVert:
                    this.soundsAudioSource.PlayOneShot(this.stripedSound);

                    break;
            }
        }

        public void PlayWinGame() {
            this.soundsAudioSource.PlayOneShot(this.levelComplete);
        }

        public void PlayLooseGame() {
            this.soundsAudioSource.PlayOneShot(this.levelLoosed);
        }
        
        public void PlaySwipe() {
            this.soundsAudioSource.PlayOneShot(this.swipedSound);
        }

        public void OnBombCreated((CandyColor color, CandyType type) valueTuple) {
            switch (valueTuple.type) {
                case CandyType.Bomb:
                    if (valueTuple.color == CandyColor.Multiple) {
                        this.soundsAudioSource.PlayOneShot(this.multipleBombCreated);
                    }
                    else {
                        this.soundsAudioSource.PlayOneShot(this.wrappedBombCreated);
                    }

                    break;

                case CandyType.StripedHor:
                    this.soundsAudioSource.PlayOneShot(this.stripedCandyCreated);

                    break;

                case CandyType.StripedVert:
                    this.soundsAudioSource.PlayOneShot(this.stripedCandyCreated);

                    break;

                default:
                    this.soundsAudioSource.PlayOneShot(this.stripedCandyCreated);

                    break;
            }
        }
    }
}