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
            if (response.status === 400) {
                const validationElement = document.getElementById('validationText');
                validationElement.textContent = 'Invalid path: ' + response.statusText;
                validationElement.style.display = 'block';
            }
            else if (response.ok) {
                loadTreeView()
            }
        })
        .catch(error => {
            console.log('error', error)
        })
}