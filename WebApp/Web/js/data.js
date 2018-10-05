const apiUrl = 'api.php?';
const native = true;

function getNews() {
	return new Promise((resolve) => {
		fetch(`${apiUrl}action=new`).then((response) => {				 
			return response.json();
		}).then((data) => {
			resolve(data);
		});
	});
}

function addNewFeed(link) {
    return new Promise((resolve) => {
		fetch(`${apiUrl}action=feed_add&link=${link}`).then((response) => {				 
			return response.json();
		}).then((data) => {
			resolve(data);
		});
    });
}

function deleteFeed(id) {
    return new Promise((resolve) => {
		fetch(`${apiUrl}action=feed_delete&id=${id}`).then((response) => {				 
			return response.json();
		}).then((data) => {
			resolve(data);
		});
	});
}

function getFeeds() {   
    return new Promise((resolve) => {
        if (native) {
            bridge.call('getFeeds').then((result) => {
                let data = JSON.parse(result);
                resolve(data);
            });
        } else {
            fetch(`${apiUrl}action=feeds`).then((response) => {
                return response.json();
            }).then((data) => {
                resolve(data);
            });
        }
	});
}

function getAllNews(from, to) {
    return new Promise((resolve) => {
        if (native) {
            bridge.call('getAllNews', { from: from, to: to }).then((result) => {
                let data = JSON.parse(result);
                resolve(data);
            });
        } else {
            fetch(`${apiUrl}action=news_all&from=${from}&to=${to}`).then((response) => {
                return response.json();
            }).then((data) => {
                resolve(data);
            });
        }
	});
}

function markAsRead(id) {
    return new Promise((resolve) => {
		fetch(`${apiUrl}action=mark_as_read&id=${id}`).then((response) => {				 
			return response.json();
		}).then((data) => {
			resolve(data);
		});
	});
}
