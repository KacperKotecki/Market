import { initSwiper } from './ui/galleryManager.js';

document.addEventListener('DOMContentLoaded', function () {
    const fileInput = document.getElementById('photosInput');
    const galleryContainer = document.getElementById('galleryContainer');
    const mainWrapper = document.getElementById('mainSwiperWrapper');
    const thumbWrapper = document.getElementById('thumbSwiperWrapper');
    const btnGenerate = document.getElementById('aiButton');
    const submitBtn = document.querySelector('input[type="submit"]');

    const spinner = document.getElementById("spinner");
    const alertBox = document.getElementById('aiErrorAlert');
    const alertMsg = document.getElementById('aiErrorMessage');
    const auctionIdInput = document.getElementById('AuctionId');
    
    let currentAuctionId = null;
    let pollInterval = null;

    if (fileInput) {
        fileInput.addEventListener('change', async function () {
            mainWrapper.innerHTML = '';
            thumbWrapper.innerHTML = '';
            const files = Array.from(this.files);

            if (files.length === 0) {
                galleryContainer.classList.add('d-none');
                return;
            }

            galleryContainer.classList.remove('d-none');

            // Podgląd lokalny
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

            // Wgrywanie w tle
            const formData = new FormData();
            for (let i = 0; i < this.files.length; i++) {
                formData.append("files", this.files[i]);
            }

            try {
                // Zablokuj submita do czasu załadowania zdjęć do API
                if(submitBtn) submitBtn.disabled = true;
                if(btnGenerate) btnGenerate.disabled = true;

                const response = await fetch('/api/auctions/draft-images', {
                    method: 'POST',
                    body: formData
                });

                if (!response.ok) {
                    throw new Error('Nie udało się wgrać zdjęć na serwer');
                }

                const data = await response.json();
                currentAuctionId = data.auctionId;
                if(currentAuctionId && auctionIdInput) {
                    auctionIdInput.value = currentAuctionId;
                }

                startPolling(currentAuctionId);

            } catch (error) {
                console.error(error);
                alertMsg.innerText = error.message;
                alertBox.classList.remove("d-none");
                if(submitBtn) submitBtn.disabled = false;
            }
        });
    }

    function startPolling(id) {
        if(pollInterval) clearInterval(pollInterval);
        
        pollInterval = setInterval(async () => {
            try {
                const response = await fetch(`/api/auctions/${id}/status`);
                if(response.ok) {
                    const data = await response.json();
                    updateUIBasedOnStatus(data);
                }
            } catch(e) {
                console.error("Polling error", e);
            }
        }, 3000);
    }

    function updateUIBasedOnStatus(data) {
        const { status, title, description, price, category, generatedByAi } = data;
        
        switch (status) {
            case "ImagesProcessing":
                if(submitBtn) submitBtn.disabled = true;
                if(btnGenerate) btnGenerate.disabled = true;
                break;
            case "ImagesReady":
                if(submitBtn) submitBtn.disabled = false;
                if(btnGenerate) btnGenerate.disabled = false;
                spinner.classList.add("d-none");
                
                // Jeśli AI właśnie skończyło i wypełniło dane (backend ustawi z powrotem ImagesReady po sukcesie)
                if (generatedByAi && title) {
                    if(!document.getElementById("Title").value) {
                         if (title) document.getElementById("Title").value = title;
                         if (description) document.getElementById("Description").value = description;
                         if (price) document.getElementById("Price").value = price;
                         
                         if (document.getElementById("generatedByAiValue")) {
                             document.getElementById("generatedByAiValue").value = "true";
                         }

                         if (category) {
                             const categorySelect = document.getElementById("Category");
                             if (categorySelect) {
                                 let options = Array.from(categorySelect.options);
                                 let optionToSelect = options.find(item => item.text.includes(category) || item.value === category);
                                 if (optionToSelect) categorySelect.value = optionToSelect.value;
                             }
                         }
                    }
                }
                break;
            case "AiProcessing":
                if(submitBtn) submitBtn.disabled = true;
                if(btnGenerate) btnGenerate.disabled = true;
                spinner.classList.remove("d-none");
                
                ['Title', 'Description', 'Price', 'Category'].forEach(id => {
                    const el = document.getElementById(id);
                    if(el) el.disabled = true;
                });
                break;
            case "AiGenerationFailed":
                if(submitBtn) submitBtn.disabled = false;
                if(btnGenerate) btnGenerate.disabled = false;
                spinner.classList.add("d-none");
                
                ['Title', 'Description', 'Price', 'Category'].forEach(id => {
                    const el = document.getElementById(id);
                    if(el) el.disabled = false;
                });

                alertMsg.innerText = "Funkcja AI nie mogła wygenerować opisu. Proszę napisać ręcznie lub spróbować ponownie.";
                alertBox.classList.remove("d-none");
                // Stop polling po błędzie AI (by nie bombardować użytkownika błędem co 3s)
                if(pollInterval) {
                   clearInterval(pollInterval);
                   pollInterval = null;
                }
                break;
        }

        // Przywracanie obsługi inputów po powrocie z AiProcessing do ImagesReady
        if (status === "ImagesReady") {
            ['Title', 'Description', 'Price', 'Category'].forEach(id => {
                const el = document.getElementById(id);
                if(el) el.disabled = false;
            });
            // Stop polling gdy gotowe, do momentu kolejnego kliknięcia
            if(pollInterval) {
                clearInterval(pollInterval);
                pollInterval = null;
            }
        }
    }

    if(btnGenerate) {
        btnGenerate.addEventListener("click", async function (e) {
            e.preventDefault();
            
            alertBox.classList.add("d-none");

            if (!currentAuctionId) {
                alertMsg.innerText = "Proszę najpierw wybrać zdjęcia i poczekać na ich przetworzenie!";
                alertBox.classList.remove("d-none");
                return;
            }

            spinner.classList.remove("d-none");
            btnGenerate.disabled = true;

            const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
            let token = "";
            if (tokenElement) token = tokenElement.value;

            try {
                const response = await fetch(`/api/ai/generate/${currentAuctionId}`, {
                    method: 'POST',
                    headers: {
                        "RequestVerificationToken": token
                    }
                });

                if(!response.ok) {
                     throw new Error('AI Generation request failed');
                }

                startPolling(currentAuctionId);

            } catch (error) {
                 console.error(error);
                 alertMsg.innerText = error.message;
                 alertBox.classList.remove("d-none");
                 spinner.classList.add("d-none");
                 btnGenerate.disabled = false;
            }
        });
    }
});