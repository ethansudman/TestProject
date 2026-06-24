function uploadFile() {
    // Clear previous validation messages
    const validationElement = document.getElementById('validationText');
    validationElement.style.display = 'none';

    const fileInput = document.getElementById('fileInput');
    const pathInput = document.getElementById('pathInput');

    var data = new FormData()
    data.append('file', fileInput.files[0])
    data.append('path', pathInput.value)

    fetch('https://localhost:7146/DirectoryBrowsing/upload', {
        method: 'POST',
        body: data
    })
        .then(response => {
            // If the response status is 400, display the validation message
            if (response.status === 400) {
                const validationElement = document.getElementById('validationText');
                validationElement.textContent = 'Invalid path: ' + response.statusText;
                validationElement.style.display = 'block';
            }
            // Otherwise, re-load the tree view to reflect the newly uploaded file
            // In the future, we could consider updating the tree view without reloading the whole thing, but for now, this is a simple solution.
            else if (response.ok) {
                loadTreeView()
            }
        })
        .catch(error => {
            console.log('error', error)
        })
}