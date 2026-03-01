export function initSwiper() {
    new Swiper(".mySwiper", {
        spaceBetween: 10,
        slidesPerView: 6,
        freeMode: true,
        watchSlidesProgress: true,
        breakpoints: {
            0: { slidesPerView: 3 },
            576: { slidesPerView: 4 },
            992: { slidesPerView: 6 }
        }
    });

    const swiperThumbs = document.querySelector(".mySwiper").swiper;

    new Swiper(".mySwiper2", {
        spaceBetween: 10,
        navigation: { nextEl: ".swiper-button-next", prevEl: ".swiper-button-prev" },
        thumbs: { swiper: swiperThumbs }
    });
}