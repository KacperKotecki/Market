document.addEventListener('DOMContentLoaded', function () {
    const fileInput = document.getElementById('photosInput');
    const galleryContainer = document.getElementById('galleryContainer');
    const mainWrapper = document.getElementById('mainSwiperWrapper');
    const thumbWrapper = document.getElementById('thumbSwiperWrapper');
    const btnGenerate = document.getElementById('aiButton');
    
    function initSwiper() {
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

    if (fileInput) {
        fileInput.addEventListener('change', function () {
            mainWrapper.innerHTML = '';
            thumbWrapper.innerHTML = '';
            const files = Array.from(this.files);

            if (files.length === 0) {
                galleryContainer.classList.add('d-none');
                return;
            }

            galleryContainer.classList.remove('d-none');

            let loadedCount = 0;
            files.forEach((file) => {
                if (!file.type.startsWith('image/')) return;
                const reader = new FileReader();
                reader.onload = function (e) {
                    mainWrapper.innerHTML += `<div class="swiper-slide"><img src="${e.target.result}" /></div>`;
                    thumbWrapper.innerHTML += `<div class="swiper-slide border border-secondary"><img src="${e.target.result}" style="object-fit: contain;" /></div>`;
                    
                    loadedCount++;
                    if(loadedCount === files.length) {
                        setTimeout(initSwiper, 100);
                    }
                };
                reader.readAsDataURL(file);
            });
        });
    }
    
    const spinner = document.getElementById("spinner");
    const alertBox = document.getElementById('aiErrorAlert');
    const alertMsg = document.getElementById('aiErrorMessage');

    if(btnGenerate) {
        btnGenerate.addEventListener("click", async function (e) {
            e.preventDefault();
            
            alertBox.classList.add("d-none");

            if (!fileInput.files.length) {
                alertMsg.innerText = "Proszę najpierw wybrać zdjęcia produktu!";
                alertBox.classList.remove("d-none");
                return;
            }

            spinner.classList.remove("d-none");
            btnGenerate.disabled = true;

            const formData = new FormData();
            for (let i = 0; i < fileInput.files.length; i++) {
                formData.append("photos", fileInput.files[i]);
            }

            try {
                const response = await fetch('/Auctions/GenerateDescription', {
                    method: 'POST',
                    body: formData
                });

                if (!response.ok) {
                    const err = await response.json();
                    throw new Error(err.error || "Wystąpił błąd serwera");
                }

                const data = await response.json();

                if (data.title && data.title.startsWith("ERROR:")) {
                        throw new Error(data.title.replace("ERROR:", "").trim());
                }

                if (data.title) document.getElementById("Title").value = data.title;
                if (data.description) document.getElementById("Description").value = data.description;
                if (data.suggestedPrice) document.getElementById("Price").value = data.suggestedPrice;
                generatedByAiValue.value = "true";

                if (data.category) {
                    const categorySelect = document.getElementById("Category");
                    let options = Array.from(categorySelect.options);
                    let optionToSelect = options.find(item => item.text.includes(data.category) || item.value === data.category);
                    if (optionToSelect) categorySelect.value = optionToSelect.value;
                }

            } catch (error) {
                console.error(error);
                alertMsg.innerText = error.message;
                alertBox.classList.remove("d-none");
            } finally {
                spinner.classList.add("d-none");
                btnGenerate.disabled = false;
            }
        });
    }

   
});