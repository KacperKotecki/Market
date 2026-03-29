function changeImage(src) {
            document.getElementById('mainImage').src = src;
        }

        document.addEventListener("DOMContentLoaded", function () {
            var swiperThumbs = new Swiper(".mySwiper", {
                spaceBetween: 10,
                slidesPerView: 4,      // Domyślnie 4 miniatury widoczne
                freeMode: true,
                watchSlidesProgress: true,
            });
            new Swiper(".mySwiper2", {
                spaceBetween: 10,
                navigation: {
                    nextEl: ".swiper-button-next",
                    prevEl: ".swiper-button-prev",
                },
                thumbs: {
                    swiper: swiperThumbs, // Powiązanie z miniaturami
                },
            });
        });