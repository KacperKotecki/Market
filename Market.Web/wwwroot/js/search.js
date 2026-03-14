document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('search-input');
    const resultsContainer = document.querySelector('#auction-results');
    const form = document.querySelector('#search-form');

    if (!searchInput || !resultsContainer || !form) {
        return;
    }

    let debounceTimer;
    const MIN_LOADING_DURATION = 1600; // ms — gwarantuje widoczność spinnera

    function setLoading(isLoading) {
        if (isLoading) {
            resultsContainer.classList.add('results-loading');
        } else {
            resultsContainer.classList.remove('results-loading');
        }
    }

    async function searchWithFilters() {
        const params = new URLSearchParams(new FormData(form));
        const startTime = Date.now();

        setLoading(true);

        try {
            const response = await fetch(`/Auctions/Search?${params.toString()}`, {
                method: 'GET'
            });

            // Czekaj aż upłynie minimum-duration, zanim wyświetlisz wynik
            const elapsedTime = Date.now() - startTime;
            if (elapsedTime < MIN_LOADING_DURATION) {
                await new Promise(resolve => 
                    setTimeout(resolve, MIN_LOADING_DURATION - elapsedTime)
                );
            }

            if (!response.ok) {
                return;
            }

            const html = await response.text();
            resultsContainer.innerHTML = html;
        } finally {
            setLoading(false);
        }
    }

    searchInput.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(searchWithFilters, 200);
    });
});
    
